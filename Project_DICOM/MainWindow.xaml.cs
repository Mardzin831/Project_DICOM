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
        int countFiles;

        List<string> valueReps = new List<string>() { "OB", "OW", "SQ" };
        NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;

        // (7fe0,0010) Pixel Data
        public float[] pixelData;

        // (0028,0010) Rows
        public int rows;

        // (0028,0011) Columns
        public int cols;

        // (0028,0101) Bits Stored
        public int bitsStored;

        // (0028,0100) Bits Allocated
        public int bitsAllocated;

        // (0028,1053) Rescale Slope
        public float rescaleSlope;

        // (0028,1052) Rescale Intercept
        public float rescaleIntercept;

        // (0028,0030) Pixel Spacing [2]
        public float[] pixelSpacing = new float[2];

        // (0018,0050) Slice Thickness
        public float sliceThickness;

        // (0020,0032) Image Position (Patient) [3]
        public float[] imagePosition = new float[3];

        // (0028,1050) Window Center
        public float windowCenter;

        // (0028,1051) Window Width
        public float windowWidth;

        // (0020,1041) Slice Location
        public float sliceLocation;

        public MainWindow()
        {
            InitializeComponent();
            RenderOptions.SetBitmapScalingMode(Image1, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(Image2, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(Image3, BitmapScalingMode.NearestNeighbor);
        }

        public void LoadFile(byte[] bytes)
        {
            bool found = false;
            int i;

            // Szukanie DICM
            for (i = 0; i + 3 < bytes.Length; i += 4)
            {
                if (bytes[i] == 'D' && bytes[i + 1] == 'I' && bytes[i + 2] == 'C' && bytes[i + 3] == 'M')
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Debug.WriteLine("Brak DICM");
                return;
            }

            int ignore;
            int length;
            int tagGroup;
            int tagNumber;

            if(countFiles == 0)
            {
                for (i += 4; i < bytes.Length; i += ignore)
                {
                    tagGroup = (bytes[i + 1] << 8) + bytes[i];
                    tagNumber = (bytes[i + 3] << 8) + bytes[i + 2];

                    ignore = 6;

                    string vr = "";
                    vr += (char)bytes[i + 4];
                    vr += (char)bytes[i + 5];

                    if (valueReps.Contains(vr))
                    {
                        length = (bytes[i + 11] << 24) + (bytes[i + 10] << 16) + (bytes[i + 9] << 8) + bytes[i + 8];
                        ignore += 4;
                    }
                    else
                    {
                        length = (bytes[i + 7] << 8) + bytes[i + 6];
                    }
                    ignore += 2 + length;

                    // (0028,0010) Rows
                    if (tagGroup == 0x0028 && tagNumber == 0x0010)
                    {
                        rows = (bytes[i + ignore - length + 1] << 8) + bytes[i + ignore - length];
                    }

                    // (0028,0011) Columns
                    if (tagGroup == 0x0028 && tagNumber == 0x0011)
                    {
                        cols = (bytes[i + ignore - length + 1] << 8) + bytes[i + ignore - length];
                    }

                    // (0028,0101) Bits Stored
                    if (tagGroup == 0x0028 && tagNumber == 0x0101)
                    {
                        bitsStored = (bytes[i + ignore - length + 1] << 8) + bytes[i + ignore - length];
                    }

                    // (0028,0100) Bits Allocated
                    if (tagGroup == 0x0028 && tagNumber == 0x0100)
                    {
                        bitsAllocated = (bytes[i + ignore - length + 1] << 8) + bytes[i + ignore - length];
                    }

                    // (0028,1053) Rescale Slope
                    if (tagGroup == 0x0028 && tagNumber == 0x1053)
                    {
                        string buffer = ReturnValues(bytes, i + ignore - length, i + ignore);
                        var parts = buffer.Split('\\');
                        float a = float.Parse(parts[0], nfi);

                        rescaleSlope = a;
                    }

                    // (0028,1052) Rescale Intercept
                    if (tagGroup == 0x0028 && tagNumber == 0x1052)
                    {
                        string buffer = ReturnValues(bytes, i + ignore - length, i + ignore);
                        var parts = buffer.Split('\\');
                        float a = float.Parse(parts[0], nfi);

                        rescaleIntercept = a;
                    }

                    // (0028,0030) Pixel Spacing [2]
                    if (tagGroup == 0x0028 && tagNumber == 0x0030)
                    {
                        string buffer = ReturnValues(bytes, i + ignore - length, i + ignore);
                        var parts = buffer.Split('\\');
                        float a = float.Parse(parts[0], nfi);
                        float b = float.Parse(parts[1], nfi);

                        pixelSpacing[0] = a;
                        pixelSpacing[1] = b;
                    }

                    //(0018,0050) Slice Thickness
                    if (tagGroup == 0x0018 && tagNumber == 0x0050)
                    {
                        string buffer = ReturnValues(bytes, i + ignore - length, i + ignore);
                        float a = float.Parse(buffer, nfi);

                        sliceThickness = a;
                    }

                    // (0020,0032) Image Position (Patient) [3]
                    if (tagGroup == 0x0020 && tagNumber == 0x0032)
                    {
                        string buffer = ReturnValues(bytes, i + ignore - length, i + ignore);
                        var parts = buffer.Split('\\');

                        float a = float.Parse(parts[0], nfi);
                        float b = float.Parse(parts[1], nfi);
                        float c = float.Parse(parts[2], nfi);

                        imagePosition[0] = a;
                        imagePosition[1] = b;
                        imagePosition[2] = c;
                    }

                    // (0028,1050) Window Center
                    if (tagGroup == 0x0028 && tagNumber == 0x1050)
                    {
                        string buffer = ReturnValues(bytes, i + ignore - length, i + ignore);
                        var parts = buffer.Split('\\');
                        float a = float.Parse(parts[0], nfi);

                        windowCenter = a;
                    }

                    // (0028,1051) Window Width
                    if (tagGroup == 0x0028 && tagNumber == 0x1051)
                    {
                        string buffer = ReturnValues(bytes, i + ignore - length, i + ignore);
                        var parts = buffer.Split('\\');
                        float a = float.Parse(parts[0], nfi);

                        windowWidth = a;
                    }

                    // (0020,1041) Slice Location
                    if (tagGroup == 0x0020 && tagNumber == 0x1041)
                    {
                        string buffer = ReturnValues(bytes, i + ignore - length, i + ignore);
                        var parts = buffer.Split('\\');
                        float a = float.Parse(parts[0], nfi);

                        sliceLocation = a;
                    }
           
                    // (7fe0,0010) Pixel Data
                    if (tagGroup == 0x7fe0 && tagNumber == 0x0010)
                    {
                        i += ignore - length;
                        break;
                    }
                }
            }
            else
            {
                for (i += 4; i < bytes.Length; i += ignore)
                {
                    tagGroup = (bytes[i + 1] << 8) + bytes[i];
                    tagNumber = (bytes[i + 3] << 8) + bytes[i + 2];

                    ignore = 6;

                    string vr = "";
                    vr += (char)bytes[i + 4];
                    vr += (char)bytes[i + 5];

                    if (valueReps.Contains(vr))
                    {
                        length = (bytes[i + 11] << 24) + (bytes[i + 10] << 16) + (bytes[i + 9] << 8) + bytes[i + 8];
                        ignore += 4;
                    }
                    else
                    {
                        length = (bytes[i + 7] << 8) + bytes[i + 6];
                    }
                    ignore += 2 + length;

                    // (7fe0,0010) Pixel Data
                    if (tagGroup == 0x7fe0 && tagNumber == 0x0010)
                    {
                        i += ignore - length;
                        break;
                    }
                }
            }
            
            // Odczytywanie pikseli z plików
            int j = 0, k = 0;
            for (; i < bytes.Length; i += 2, k++)
            {
                float color = (((bytes[i + 1]) << 8) + bytes[i]) * rescaleSlope + rescaleIntercept;
                float center = windowCenter - 0.5f;
                float range = windowWidth - 1.0f;
                byte min = 0;
                byte max = 255;

                // Wzory z dokumentacji
                if (color <= (center - range / 2.0f))
                {
                    pixelData[countFiles * width * height + j * width + k] = min;
                }
                else if (color > (center + range / 2.0f))
                {
                    pixelData[countFiles * width * height + j * width + k] = max;
                }
                else
                {
                    pixelData[countFiles * width * height + j * width + k] = ((color - center) / range + 0.5f) * (max - min) + min;
                }

                if (k == width)
                {
                    k = 0;
                    j++;
                }
            }
        }

        // Zwraca string z wartościami dla VR, które są w postaci stringów
        public string ReturnValues(byte[] bytes, int start, int end)
        {
            char[] buffer = new char[end - start + 1];
            int i = 0;
            for (; bytes[start] != bytes[end]; start++)
            {
                buffer[i] = (char)bytes[start];
                i++;
            }
            
            return new string(buffer);
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

        public void DrawImage1()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            int sliderValue = (int)slider1.Value;
            bitMap1 = new byte[stride * height];

            Parallel.For(0, width, i =>
            {
                for (int j = 0; j < height; j++)
                {
                    SetBitMap(bitMap1, i, j, pixelData[sliderValue * width * height + i * height + j]);
                }
            });

            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap1, stride);

            //var bitmap = new TransformedBitmap(bs, 
            //    new ScaleTransform(1, 1));
            Image1.Source = bs;
        }
        public void DrawImage2()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            int sliderValue = (int)slider2.Value;
            bitMap2 = new byte[stride * height];

            Parallel.For(0, countFiles, i =>
            {
                for (int j = 0; j < height; j++)
                {
                    SetBitMap(bitMap2, countFiles - 1 - i, j, pixelData[i * width * height + sliderValue * width + j]);
                }
            });
            BitmapSource bs = BitmapSource.Create(width, countFiles, 96d, 96d, pf, null, bitMap2, stride);

            CroppedBitmap croppedBitmap = new CroppedBitmap(bs, new Int32Rect(0, 0, width, countFiles));
            var bitmap = new TransformedBitmap(croppedBitmap,
                new ScaleTransform(1, 512.0 / countFiles));
            Image2.Source = bitmap;
        }

        public void DrawImage3()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            int sliderValue = (int)slider3.Value;
            bitMap3 = new byte[stride * height];

            Parallel.For(0, countFiles, i =>
            {
                for (int j = 0; j < width; j++)
                {
                    SetBitMap(bitMap3, countFiles - 1 - i, j, pixelData[i * width * height + j * width + sliderValue]);
                }
            });

            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap3, stride);
            
            CroppedBitmap croppedBitmap = new CroppedBitmap(bs, new Int32Rect(0, 0, width, countFiles));
            var bitmap = new TransformedBitmap(croppedBitmap,
                new ScaleTransform(1, 512.0 / countFiles));
            Image3.Source = bitmap;
        }
        public void DrawImages()
        {
            DrawImage1();
            DrawImage2();
            DrawImage3();
        }

        public void SetBitMap(byte[] bitMap, int x, int y, float color)
        {
            bitMap[512 * 4 * x + 4 * y] = (byte)color;
            bitMap[512 * 4 * x + 4 * y + 1] = (byte)color;
            bitMap[512 * 4 * x + 4 * y + 2] = (byte)color;
        }

        private void OnSlide1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(countFiles > 0)
            {
                DrawImage1();
            }
        }

        private void OnSlide2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                DrawImage2();
            }
        }

        private void OnSlide3(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (countFiles > 0)
            {
                DrawImage3();
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
            float spacing = pixelSpacing[0];
            double distance;
            if (line1.Visibility == Visibility.Visible && label1.Visibility == Visibility.Visible)
            {
                line1.Visibility = Visibility.Hidden;
                label1.Visibility = Visibility.Hidden;
            }
            else if(line1.Visibility == Visibility.Hidden && label1.Visibility == Visibility.Hidden)
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
                if(distance >= 10)
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
            float spacing = pixelSpacing[1];
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
            float spacing = pixelSpacing[0];
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

                slider2.Value = (int)slider1.Value * 512.0 / 112.0;
                slider3.Value = (int)slider1.Value * 512.0 / 112.0;

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

                slider1.Value = (int)slider2.Value * 112.0 / 512.0;
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

                slider1.Value = (int)slider3.Value * 112.0 / 512.0;
                slider2.Value = slider3.Value;

                spot1.Visibility = Visibility.Visible;
                spot2.Visibility = Visibility.Visible;
                spot3.Visibility = Visibility.Visible;
            }
        }

        private void OnPickFolder(object sender, RoutedEventArgs e)
        {
            string directory = "";
            var fbd = new FolderBrowserDialog();
            byte[] fileBytes;
            fbd.SelectedPath = @"D:\infa_studia\10semestr\WZI\head-dicom\";
            DialogResult result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                directory = fbd.SelectedPath;
            }
            else
            {
                return;
            }

            IEnumerable<string> files = Directory.EnumerateFiles(directory);
            List<string> lof = new List<string>(files);

            // Sortowanie plików według liczb na końcu ścieżki
            lof.Sort((s1, s2) => Int32.Parse(Regex.Match(s1, @"(\d+)(?!.*\d)").Value).
                CompareTo(Int32.Parse(Regex.Match(s2, @"(\d+)(?!.*\d)").Value)));

            countFiles = 0;
            int allFiles = files.Count();
            if (allFiles < 1) return;
            pixelData = new float[allFiles * width * height];

            foreach (string file in lof)
            {
                fileBytes = File.ReadAllBytes(file);
                
                LoadFile(fileBytes);
                countFiles += 1;
            }
            SetSliders();
            DrawImages();
        }
    }
}
