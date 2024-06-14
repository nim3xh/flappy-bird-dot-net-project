using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace FlappyBirdGame_mk
{
    public class ImageSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double canvasSize = System.Convert.ToDouble(value);
            // You can adjust the image size based on the canvas size here
            return canvasSize * 0.8; // Adjust the factor as needed
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed partial class GamePage : Page
    {
        private DispatcherTimer timer;
        private Random random;
        private List<Image> pipes;
        private double birdVelocity;
        private bool isJumping;
        private int score;
        private double gravity = 0.3; // Gravity acceleration
        private double terminalVelocity = 10; // Maximum falling speed
        private int tickCount;


        public GamePage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
            this.Loaded += GamePage_Loaded;
            this.KeyDown += GamePage_KeyDown; // Handle key down event
        }

        private void GamePage_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeGame();
            Focus(FocusState.Programmatic); // Set focus to the page
        }


        private void InitializeGame()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(20);
            timer.Tick += Timer_Tick;

            random = new Random();
            pipes = new List<Image>();

            birdVelocity = 0;
            isJumping = false;
            score = 0;

            tickCount = 0;

            StartGame();
        }

        private void StartGame()
        {
            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            tickCount++;
            MoveBackground();
            MoveBird();
            MovePipes();
            CheckCollision();
        }


        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            // Restart the game logic here
            RestartGame();
        }

        private void RestartGame()
        {
            // Reset game variables
            birdVelocity = 0;
            isJumping = false;
            score = 0;
            tickCount = 0;

            // Remove all pipes from the canvas
            pipes.ForEach(pipe => GameCanvas.Children.Remove(pipe));
            pipes.Clear();

            // Reset bird position
            Canvas.SetTop(BirdImage, GameCanvas.ActualHeight / 2 - BirdImage.ActualHeight / 2);

            // Restart the game timer
            timer.Start();

            // Reset score text
            ScoreTextBlock.Text = "0";
            ScoreTextBlock.Visibility = Visibility.Visible;
            ScoreLable.Visibility = Visibility.Visible;

            // Hide the game over screen
            GameOverGrid.Visibility = Visibility.Collapsed;
        }


        private void MoveBackground()
        {
            // Move the first background image to the left
            Canvas.SetLeft(BackgroundImage, Canvas.GetLeft(BackgroundImage) - 2);

            // Move the second background image to the left
            Canvas.SetLeft(BackgroundImage2, Canvas.GetLeft(BackgroundImage2) - 2);

            // If the first background image has moved completely out of view, reset its position
            if (Canvas.GetLeft(BackgroundImage) + BackgroundImage.ActualWidth <= 0)
            {
                Canvas.SetLeft(BackgroundImage, Canvas.GetLeft(BackgroundImage2) + BackgroundImage2.ActualWidth);
            }

            // If the second background image has moved completely out of view, reset its position
            if (Canvas.GetLeft(BackgroundImage2) + BackgroundImage2.ActualWidth <= 0)
            {
                Canvas.SetLeft(BackgroundImage2, Canvas.GetLeft(BackgroundImage) + BackgroundImage.ActualWidth);
            }
        }




        private void MoveBird()
        {
            if (isJumping)
            {
                birdVelocity = -5; // Jumping speed
                isJumping = false;
                SwooshSound.Play(); // Play swoosh sound effect
            }
            else
            {
                // Apply gravity
                birdVelocity += gravity;
                if (birdVelocity > terminalVelocity)
                {
                    birdVelocity = terminalVelocity; // Limit falling speed
                }
            }

            double newY = Canvas.GetTop(BirdImage) + birdVelocity;
            Canvas.SetTop(BirdImage, newY);

            // Rotate bird based on velocity
            BirdImage.RenderTransform = new RotateTransform { CenterX = BirdImage.Width / 2, CenterY = BirdImage.Height / 2, Angle = birdVelocity * 3 };
        }

        private void MovePipes()
        {
            List<Image> pipesToRemove = new List<Image>();

            foreach (var pipe in pipes)
            {
                double newX = Canvas.GetLeft(pipe) - 2; // Pipe movement speed
                Canvas.SetLeft(pipe, newX);

                // Check if the pipe is off the screen
                if (newX < -pipe.Width)
                {
                    pipesToRemove.Add(pipe);
                }

                // Check if the pipe is close to the bird
                if (Canvas.GetLeft(pipe) < Canvas.GetLeft(BirdImage) + BirdImage.Width &&
                    Canvas.GetLeft(pipe) + pipe.Width > Canvas.GetLeft(BirdImage) &&
                    Canvas.GetTop(pipe) < Canvas.GetTop(BirdImage) + BirdImage.Height &&
                    Canvas.GetTop(pipe) + pipe.Height > Canvas.GetTop(BirdImage))
                {
                    GameOver();
                    break;
                }

                // Check if the pipe has been passed
                if (Canvas.GetLeft(pipe) + pipe.Width < Canvas.GetLeft(BirdImage))
                {
                    // Pipe has been passed
                    UpdateScore();
                }
            }

            foreach (var pipeToRemove in pipesToRemove)
            {
                GameCanvas.Children.Remove(pipeToRemove);
                pipes.Remove(pipeToRemove);
            }

            // Add new pipes every 100 frames
            if (tickCount % 100 == 0)
            {
                AddPipe();
            }
        }




        private void AddPipe()
        {
            int pipeWidth = 70;
            int pipeHeight = 250;
            int gapHeight = 270; // Set the height of the gap between pipes

            // Randomly determine the gap position
            int maxGapPosition = (int)GameCanvas.ActualHeight - pipeHeight - gapHeight;
            int gapPosition = random.Next(100, maxGapPosition);

            // Add top pipe
            Image topPipe = new Image();
            topPipe.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/pipe.png"));
            topPipe.Width = pipeWidth;
            topPipe.Height = pipeHeight; // Set the height of the top pipe
            Canvas.SetTop(topPipe, 0);
            Canvas.SetLeft(topPipe, GameCanvas.ActualWidth); // Start from the right edge of the canvas
            RotatePipe(topPipe); // Rotate the top pipe
            GameCanvas.Children.Add(topPipe);
            pipes.Add(topPipe);

            // Add bottom pipe
            Image bottomPipe = new Image();
            bottomPipe.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/pipe.png"));
            bottomPipe.Width = pipeWidth;
            bottomPipe.Height = pipeHeight; // Set the height of the bottom pipe
            Canvas.SetTop(bottomPipe, gapPosition + gapHeight); // Gap position plus gap height
            Canvas.SetLeft(bottomPipe, GameCanvas.ActualWidth); // Start from the right edge of the canvas
            GameCanvas.Children.Add(bottomPipe);
            pipes.Add(bottomPipe);
        }


        private void RotatePipe(Image pipe)
        {
            // Rotate the upper pipes
            if (Canvas.GetTop(pipe) == 0)
            {
                pipe.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
                RotateTransform rotateTransform = new RotateTransform();
                rotateTransform.Angle = 180;
                pipe.RenderTransform = rotateTransform;
            }
        }


        private void CheckCollision()
        {
            // Check collision with ground
            if (Canvas.GetTop(BirdImage) + BirdImage.Height > GameCanvas.ActualHeight)
            {
                GameOver();
                //CollisionSound.Play(); // Play collision sound
                HitSound.Play(); // Play hit sound effect
                DieSound.Play(); // Play die sound effect
            }

            // Check collision with pipes
            foreach (var pipe in pipes)
            {
                // Check if bird is between the pipe horizontally and vertically
                if (Canvas.GetLeft(BirdImage) + BirdImage.Width > Canvas.GetLeft(pipe) &&
                    Canvas.GetLeft(BirdImage) < Canvas.GetLeft(pipe) + pipe.Width &&
                    Canvas.GetTop(BirdImage) + BirdImage.Height > Canvas.GetTop(pipe) &&
                    Canvas.GetTop(BirdImage) < Canvas.GetTop(pipe) + pipe.Height)
                {
                    // Bird is within pipe boundaries, but not inside the gap
                    if (Canvas.GetTop(BirdImage) + BirdImage.Height < Canvas.GetTop(pipe) + 50 ||
                        Canvas.GetTop(BirdImage) > Canvas.GetTop(pipe) + pipe.Height - 50)
                    {
                        GameOver();
                        //CollisionSound.Play(); // Play collision sound
                        HitSound.Play(); // Play hit sound effect
                        DieSound.Play(); // Play die sound effect
                        break;
                    }
                }
            }
        }





        private void UpdateScore()
        {
            // Increment score every 100 frames
            if (tickCount % 100 == 0)
            {
                score++;
                ScoreTextBlock.Text = score.ToString();
            }
        }

        private void GameOver()
        {
            timer.Stop(); // Stop the game timer

            pipes.ForEach(pipe => GameCanvas.Children.Remove(pipe)); // Remove all pipes from the canvas
            ScoreTextBlock.Visibility = Visibility.Collapsed; // Hide the score text
            ScoreLable.Visibility = Visibility.Collapsed; // Hide the score label
            GameOverGrid.Visibility = Visibility.Visible; // Display the game over screen
            // Display the final score
            FinalScoreTextBlock.Text = score.ToString();
        }

        private void GamePage_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                isJumping = true; // Jump when space key is pressed
            }
        }

        private void JumpButton_Click(object sender, RoutedEventArgs e)
        {
            isJumping = true;
            WingtSound.Play(); // Play wing sound effect
        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }
        private void ScoreTextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // Your event handling code here
        }

        private void TextBlock_SelectionChanged_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
