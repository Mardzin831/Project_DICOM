using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Project_DICOM
{
    public partial class MainWindow : Window
    {
        byte[] bitMap1;
        byte[] bitMap2;
        byte[] bitMap3;
        int width = 512, height = 512;
        int countFiles = 247;
        float[] pixels;
        bool loaded = false;

        string view = "None";
        int isView = 0;
        int hits1 = 0;
        int hits2 = 0;

        int l = 0;

        // (7fe0,0010) Pixel Data
        public float[] pixelData;

        // (0028,1053) Rescale Slope
        public float rescaleSlope1 = 1;
        public float rescaleSlope2 = 1;

        // (0028,1052) Rescale Intercept
        public float rescaleIntercept1 = -3024;
        public float rescaleIntercept2 = 0;

        // (0028,1050) Window Center
        public float windowCenter1 = 30;
        public float windowCenter2 = 156;

        // (0028,1051) Window Width
        public float windowWidth1 = 300;
        public float windowWidth2 = 368;

        public MainWindow()
        {
            InitializeComponent();
            RenderOptions.SetBitmapScalingMode(Image1, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(Image2, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(Image3, BitmapScalingMode.NearestNeighbor);
            comboBox.Items.Add("None");
            comboBox.Items.Add("Mip");
            comboBox.Items.Add("Avg");
            comboBox.Items.Add("FirstHit");
            comboBox.SelectedItem = "None";
        }

        public void LoadFile(byte[] bytes, float rescaleSlope, float rescaleIntercept, float windowCenter, float windowWidth)
        {
            // Odczytywanie pikseli z plików
            int i = 0;
            for (; i < bytes.Length; i += 2, l++)
            {
                float color = (short)(((bytes[i + 1]) << 8) + bytes[i]) * rescaleSlope + rescaleIntercept;
                float center = windowCenter - 0.5f;
                float range = windowWidth - 1.0f;
                byte min = 0;
                byte max = 255;

                // Wzory z dokumentacji
                if (color <= (center - range / 2.0f))
                {
                    pixelData[l] = min;
                }
                else if (color > (center + range / 2.0f))
                {
                    pixelData[l] = max;
                }
                else
                {
                    pixelData[l] = ((color - center) / range + 0.5f) * (max - min) + min;
                }

                pixels[l] = (short)(((bytes[i + 1]) << 8) + bytes[i]);
            }
        }

        public void SetColors(float rescaleSlope, float rescaleIntercept, float windowCenter, float windowWidth)
        {
            for (int i = 0; i < countFiles; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    for (int k = 0; k < height; k++)
                    {
                        float color = (short)pixels[i * width * height + j * width + k] * rescaleSlope + rescaleIntercept;
                        float center = windowCenter - 0.5f + (int)sliderLevel.Value;
                        float range = windowWidth - 1.0f + (int)sliderWidth.Value;
                        byte min = 0;
                        byte max = 255;

                        // Wzory z dokumentacji
                        if (color <= (center - range / 2.0f))
                        {
                            pixelData[i * width * height + j * width + k] = min;
                        }
                        else if (color > (center + range / 2.0f))
                        {
                            pixelData[i * width * height + j * width + k] = max;
                        }
                        else
                        {
                            pixelData[i * width * height + j * width + k] = ((color - center) / range + 0.5f) * (max - min) + min;
                        }
                    }
                }
            }
        }
        public void SetSliders()
        {
            slider1.Maximum = countFiles - 1;
            slider2.Maximum = width - 1;
            slider3.Maximum = height - 1;

            slider1.Value = 0;
            slider2.Value = 0;
            slider3.Value = 0;
        }

        public void DrawImage1(float[] viewPixels)
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            int sliderValue = (int)slider1.Value;
            bitMap1 = new byte[stride * height];

            if (isView == 0)
            {
                Parallel.For(0, width, i =>
                {
                    for (int j = 0; j < height; j++)
                    {
                        SetBitMap(bitMap1, i, j, viewPixels[sliderValue * width * height + i * height + j]);
                    }
                });
            }
            else
            {
                Parallel.For(0, width, i =>
                {
                    for (int j = 0; j < height; j++)
                    {
                        SetBitMap(bitMap1, i, j, viewPixels[i * height + j]);
                    }
                });
            }

            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap1, stride);

            //var bitmap = new TransformedBitmap(bs, 
            //    new ScaleTransform(1, 1));
            Image1.Source = bs;
        }
        public void DrawImage2(float[] viewPixels)
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            int sliderValue = (int)slider2.Value;
            bitMap2 = new byte[stride * height];

            if (isView == 0)
            {
                Parallel.For(0, countFiles, i =>
                {
                    for (int j = 0; j < height; j++)
                    {
                        SetBitMap(bitMap2, countFiles - 1 - i, j, viewPixels[i * width * height + sliderValue * width + j]);
                    }
                });
            }
            else
            {
                Parallel.For(0, countFiles, i =>
                {
                    for (int j = 0; j < height; j++)
                    {
                        SetBitMap(bitMap2, countFiles - 1 - i, j, viewPixels[j * countFiles + i]);
                    }
                });
            }
            BitmapSource bs = BitmapSource.Create(width, countFiles, 96d, 96d, pf, null, bitMap2, stride);

            CroppedBitmap croppedBitmap = new CroppedBitmap(bs, new Int32Rect(0, 0, width, countFiles));
            var bitmap = new TransformedBitmap(croppedBitmap,
                new ScaleTransform(1, 512.0 / countFiles));
            Image2.Source = bitmap;
        }

        public void DrawImage3(float[] viewPixels)
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            int sliderValue = (int)slider3.Value;
            bitMap3 = new byte[stride * height];

            if (isView == 0)
            {
                Parallel.For(0, countFiles, i =>
                {
                    for (int j = 0; j < width; j++)
                    {
                        SetBitMap(bitMap3, countFiles - 1 - i, j, viewPixels[i * width * height + j * width + sliderValue]);
                    }
                });
            }
            else
            {
                Parallel.For(0, countFiles, i =>
                {
                    for (int j = 0; j < width; j++)
                    {
                        SetBitMap(bitMap3, countFiles - 1 - i, j, viewPixels[j * countFiles + i]);
                    }
                });
            }

            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap3, stride);

            CroppedBitmap croppedBitmap = new CroppedBitmap(bs, new Int32Rect(0, 0, width, countFiles));
            var bitmap = new TransformedBitmap(croppedBitmap,
                new ScaleTransform(1, 512.0 / countFiles));
            Image3.Source = bitmap;
        }
        public void DrawImages()
        {
            if (loaded == false)
            {
                return;
            }
            //SetColors(rescaleSlope1, rescaleIntercept1, windowCenter1, windowWidth1);

            if (view == "None")
            {
                isView = 0;
                DrawImage1(pixelData);
                DrawImage2(pixelData);
                DrawImage3(pixelData);
            }
            else
            {
                float[] pixels1 = new float[width * height];
                float[] pixels2 = new float[width * countFiles];
                float[] pixels3 = new float[height * countFiles];
                bool[] firstHit1 = new bool[width * height];
                bool[] firstHit2 = new bool[width * countFiles];
                bool[] firstHit3 = new bool[height * countFiles];

                isView = 1;
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        pixels1[i * height + j] = 0;
                        firstHit1[i * height + j] = false;
                    }
                    for (int j = 0; j < countFiles; j++)
                    {
                        pixels2[i * countFiles + j] = 0;
                        firstHit2[i * countFiles + j] = false;
                    }
                    for (int j = 0; j < countFiles; j++)
                    {
                        pixels3[i * countFiles + j] = 0;
                        firstHit3[i * countFiles + j] = false;
                    }
                }

                if (view == "Mip")
                {
                    for (int i = 0; i < countFiles; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            for (int k = 0; k < height; k++)
                            {
                                pixels1[j * height + k] = Math.Max(pixels1[j * height + k], pixelData[i * width * height + j * width + k]);
                                pixels2[k * countFiles + i] = Math.Max(pixels2[k * countFiles + i], pixelData[i * width * height + j * width + k]);
                                pixels3[j * countFiles + i] = Math.Max(pixels3[j * countFiles + i], pixelData[i * width * height + j * width + k]);
                            }
                        }
                    }
                }

                if (view == "Avg")
                {
                    for (int i = 0; i < countFiles; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            for (int k = 0; k < height; k++)
                            {
                                pixels1[j * height + k] = pixels1[j * height + k] + pixelData[i * width * height + j * width + k];
                                pixels2[k * countFiles + i] = pixels2[k * countFiles + i] + pixelData[i * width * height + j * width + k];
                                pixels3[j * countFiles + i] = pixels3[j * countFiles + i] + pixelData[i * width * height + j * width + k];
                            }
                        }
                    }
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            pixels1[i * height + j] = pixels1[i * height + j] / countFiles;
                        }
                    }
                    for (int i = 0; i < countFiles; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            pixels2[j * countFiles + i] = pixels2[j * countFiles + i] / width;
                        }
                    }
                    for (int i = 0; i < countFiles; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            pixels3[j * countFiles + i] = pixels3[j * countFiles + i] / height;
                        }
                    }
                }

                /*if (view == "FirstHit")
                {
                    for (int i = 0; i < countFiles; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            for (int k = 0; k < height; k++)
                            {
                                if (!firstHit1[j * height + k] && hits < pixelData[i * width * height + j * width + k])
                                {
                                    pixels1[j * height + k] = pixelData[i * width * height + j * width + k];
                                    firstHit1[j * height + k] = true;
                                }

                                if (!firstHit2[k * countFiles + i] && hits < pixelData[i * width * height + j * width + k])
                                {
                                    pixels2[k * countFiles + i] = pixelData[i * width * height + j * width + k];
                                    firstHit2[k * countFiles + i] = true;
                                }

                                if (!firstHit3[j * countFiles + i] && hits < pixelData[i * width * height + j * width + k])
                                {
                                    pixels3[j * countFiles + i] = pixelData[i * width * height + j * width + k];
                                    firstHit3[j * countFiles + i] = true;
                                }
                            }
                        }
                    }
                }*/

                DrawImage1(pixels1);
                DrawImage2(pixels2);
                DrawImage3(pixels3);
            }
        }

        public void SetBitMap(byte[] bitMap, int x, int y, float color)
        {
            bitMap[512 * 4 * x + 4 * y] = (byte)color;
            bitMap[512 * 4 * x + 4 * y + 1] = (byte)color;
            bitMap[512 * 4 * x + 4 * y + 2] = (byte)color;
        }

        private void OnSlide1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                DrawImage1(pixelData);
            }
        }

        private void OnSlide2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                DrawImage2(pixelData);
            }
        }

        private void OnSlide3(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                DrawImage3(pixelData);
            }
        }

        private void OnSlideLevel(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                DrawImages();
            }
        }

        private void OnSlideWidth(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                DrawImages();
            }
        }

        private void OnLeftClick1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            float spacing = 0.447266f;
            double distance;
            if (line1.Visibility == Visibility.Visible && label1.Visibility == Visibility.Visible)
            {
                line1.Visibility = Visibility.Hidden;
                label1.Visibility = Visibility.Hidden;
            }
            else if (line1.Visibility == Visibility.Hidden && label1.Visibility == Visibility.Hidden)
            {
                Point point = e.GetPosition(Image1);
                line1.X1 = point.X;
                line1.Y1 = point.Y;
                line1.X2 = point.X + 1;
                line1.Y2 = point.Y + 1;
                line1.Visibility = Visibility.Visible;
            }
            else
            {
                Point point = e.GetPosition(Image1);
                line1.X2 = point.X;
                line1.Y2 = point.Y;
                distance = Math.Sqrt((line1.X2 - line1.X1) * (line1.X2 - line1.X1) + (line1.Y2 - line1.Y1) * (line1.Y2 - line1.Y1)) * spacing;

                label1.Visibility = Visibility.Visible;
                label1.Margin = new Thickness((line1.X1 + line1.X2) / 2, (line1.Y1 + line1.Y2) / 2, 0, 0);
                if (distance >= 10)
                {
                    label1.Content = (distance / 10).ToString("F2") + " cm";
                }
                else
                {
                    label1.Content = distance.ToString("F2") + " mm";
                }
            }
        }

        private void OnLeftClick2(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            float spacing = 0.447266f;
            double scale = countFiles / 100.0;
            double distance;

            if (line2.Visibility == Visibility.Visible && label2.Visibility == Visibility.Visible)
            {
                line2.Visibility = Visibility.Hidden;
                label2.Visibility = Visibility.Hidden;
            }
            else if (line2.Visibility == Visibility.Hidden && label2.Visibility == Visibility.Hidden)
            {
                Point point = e.GetPosition(Image2);
                line2.X1 = point.X;
                line2.Y1 = point.Y;
                line2.X2 = point.X + 1;
                line2.Y2 = point.Y + 1;
                line2.Visibility = Visibility.Visible;
            }
            else
            {
                Point point = e.GetPosition(Image2);
                line2.X2 = point.X;
                line2.Y2 = point.Y;
                distance = Math.Sqrt((line2.X2 - line2.X1) * (line2.X2 - line2.X1) + (line2.Y2 - line2.Y1) * (line2.Y2 - line2.Y1) * scale * scale) * spacing;

                label2.Visibility = Visibility.Visible;
                label2.Margin = new Thickness((line2.X1 + line2.X2) / 2, (line2.Y1 + line2.Y2) / 2, 0, 0);
                if (distance >= 10)
                {
                    label2.Content = (distance / 10).ToString("F2") + " cm";
                }
                else
                {
                    label2.Content = distance.ToString("F2") + " mm";
                }
            }
        }

        private void OnLeftClick3(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            float spacing = 0.625f;
            double scale = countFiles / 100.0;
            double distance;

            if (line3.Visibility == Visibility.Visible && label3.Visibility == Visibility.Visible)
            {
                line3.Visibility = Visibility.Hidden;
                label3.Visibility = Visibility.Hidden;
            }
            else if (line3.Visibility == Visibility.Hidden && label3.Visibility == Visibility.Hidden)
            {
                Point point = e.GetPosition(Image3);
                line3.X1 = point.X;
                line3.Y1 = point.Y;
                line3.X2 = point.X + 1;
                line3.Y2 = point.Y + 1;
                line3.Visibility = Visibility.Visible;
            }
            else
            {
                Point point = e.GetPosition(Image3);
                line3.X2 = point.X;
                line3.Y2 = point.Y;
                distance = Math.Sqrt((line3.X2 - line3.X1) * (line3.X2 - line3.X1) + (line3.Y2 - line3.Y1) * (line3.Y2 - line3.Y1) * scale * scale) * spacing;

                label3.Visibility = Visibility.Visible;
                label3.Margin = new Thickness((line3.X1 + line3.X2) / 2, (line3.Y1 + line3.Y2) / 2, 0, 0);
                if (distance >= 10)
                {
                    label3.Content = (distance / 10).ToString("F2") + " cm";
                }
                else
                {
                    label3.Content = distance.ToString("F2") + " mm";
                }
            }
        }

        private void OnRightClick1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (spot1.Visibility == Visibility.Visible)
            {
                spot1.Visibility = Visibility.Hidden;
                spot2.Visibility = Visibility.Hidden;
                spot3.Visibility = Visibility.Hidden;
            }
            else if (spot1.Visibility == Visibility.Hidden)
            {
                Point point = e.GetPosition(Image1);

                spot1.X1 = point.X - 2;
                spot1.Y1 = point.Y - 2;
                spot1.X2 = point.X + 2;
                spot1.Y2 = point.Y + 2;

                spot2.X1 = point.X - 2;
                spot2.Y1 = 0;
                spot2.X2 = point.X + 2;
                spot2.Y2 = 512;

                spot3.X1 = point.Y - 2;
                spot3.Y1 = 0;
                spot3.X2 = point.Y + 2;
                spot3.Y2 = 512;

                slider2.Value = (int)slider1.Value * 512.0 / 247.0;
                slider3.Value = (int)slider1.Value * 512.0 / 247.0;

                spot1.Visibility = Visibility.Visible;
                spot2.Visibility = Visibility.Visible;
                spot3.Visibility = Visibility.Visible;
            }
        }

        private void OnRightClick2(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (spot1.Visibility == Visibility.Visible)
            {
                spot1.Visibility = Visibility.Hidden;
                spot2.Visibility = Visibility.Hidden;
                spot3.Visibility = Visibility.Hidden;
            }
            else if (spot1.Visibility == Visibility.Hidden)
            {
                Point point = e.GetPosition(Image2);

                spot1.X1 = point.X - 2;
                spot1.Y1 = 0;
                spot1.X2 = point.X + 2;
                spot1.Y2 = 512;

                spot2.X1 = point.X - 2;
                spot2.Y1 = point.Y - 2;
                spot2.X2 = point.X + 2;
                spot2.Y2 = point.Y + 2;

                spot3.X1 = 0;
                spot3.Y1 = point.Y - 2;
                spot3.X2 = 512;
                spot3.Y2 = point.Y + 2;

                slider1.Value = (int)slider2.Value * 247.0 / 512.0;
                slider3.Value = slider2.Value;

                spot1.Visibility = Visibility.Visible;
                spot2.Visibility = Visibility.Visible;
                spot3.Visibility = Visibility.Visible;
            }
        }

        private void OnRightClick3(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (spot1.Visibility == Visibility.Visible)
            {
                spot1.Visibility = Visibility.Hidden;
                spot2.Visibility = Visibility.Hidden;
                spot3.Visibility = Visibility.Hidden;
            }
            else if (spot1.Visibility == Visibility.Hidden)
            {
                Point point = e.GetPosition(Image3);

                spot1.X1 = 0;
                spot1.Y1 = point.X - 2;
                spot1.X2 = 512;
                spot1.Y2 = point.X + 2;

                spot2.X1 = 0;
                spot2.Y1 = point.Y - 2;
                spot2.X2 = 512;
                spot2.Y2 = point.Y + 2;

                spot3.X1 = point.X - 2;
                spot3.Y1 = point.Y - 2;
                spot3.X2 = point.X + 2;
                spot3.Y2 = point.Y + 2;

                slider1.Value = (int)slider3.Value * 247.0 / 512.0;
                slider2.Value = slider3.Value;

                spot1.Visibility = Visibility.Visible;
                spot2.Visibility = Visibility.Visible;
                spot3.Visibility = Visibility.Visible;
            }
        }

        private void OnComboBox(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            view = comboBox.SelectedItem.ToString();
            DrawImages();
            if (view == "None")
            {
                slider1.IsEnabled = true;
                slider2.IsEnabled = true;
                slider3.IsEnabled = true;
            }
            else
            {
                slider1.IsEnabled = false;
                slider2.IsEnabled = false;
                slider3.IsEnabled = false;
            }
        }

        private void OnTextBox1(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(textBox1.Text, out int val) == false)
            {
                return;
            }
            hits1 = val;
            if (view == "FirstHit")
            {
                DrawImages();
            }
        }

        private void OnTextBox2(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(textBox2.Text, out int val) == false)
            {
                return;
            }
            hits2 = val;
            if (view == "FirstHit")
            {
                DrawImages();
            }
        }

        private void OnSlideTransparent(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void OnPickFolder(object sender, RoutedEventArgs e)
        {
            string file1;
            string file2;
            var ofd = new OpenFileDialog();
            byte[] fileBytes1;
            byte[] fileBytes2;
            ofd.InitialDirectory = @"D:\infa_studia\10semestr\WZI\dane_zad2\";
            ofd.Multiselect = true;
            DialogResult result = ofd.ShowDialog();


            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(ofd.FileName) && ofd.FileNames.Length == 2)
            {
                file1 = ofd.FileNames[0];
                file2 = ofd.FileNames[1];
            }
            else
            {
                return;
            }

            pixelData = new float[2 * countFiles * width * height];
            pixels = new float[2 * countFiles * width * height];

            fileBytes1 = File.ReadAllBytes(file1);
            fileBytes2 = File.ReadAllBytes(file2);

            LoadFile(fileBytes2, rescaleSlope2, rescaleIntercept2, windowCenter2, windowWidth2);
            LoadFile(fileBytes1, rescaleSlope1, rescaleIntercept1, windowCenter1, windowWidth1);
            
            loaded = true;

            SetSliders();
            DrawImages();
        }
    }
}
