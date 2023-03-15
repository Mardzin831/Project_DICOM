using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Project_DICOM
{
    public partial class MainWindow : Window
    {
        List<Dicom> dicoms = new List<Dicom>();
        byte[] bitMap1;
        byte[] bitMap2;
        byte[] bitMap3;
        byte[][][]pixels;
        int width = 512, height = 512;
        int countFiles = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetSliders()
        {
            slider1.Maximum = countFiles - 1;
            slider2.Maximum = dicoms[0].rows - 1;
            slider3.Maximum = dicoms[0].cols - 1;

            slider1.Value = 0;
            slider2.Value = 0;
            slider3.Value = 0;
        }

        public void DrawImage1()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            bitMap1 = new byte[stride * height];
            FillPixels();
            for (int i = 0; i < dicoms[0].rows; i++)
            {
                for (int j = 0; j < dicoms[0].cols; j++)
                {
                    SetBitMap(bitMap1, i, j, pixels[(int)slider1.Value][i][j]);
                }
            }

            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap1, stride);
            RenderOptions.SetBitmapScalingMode(bs, BitmapScalingMode.NearestNeighbor);
            //var bitmap = new TransformedBitmap(bs, 
            //    new ScaleTransform(1, 1));
            Image1.Source = bs;
        }
        public void DrawImage2()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            bitMap2 = new byte[stride * height];
            FillPixels();
            for (int i = 0; i < countFiles; i++)
            {
                for (int j = 0; j < dicoms[0].cols; j++)
                {
                    SetBitMap(bitMap2, countFiles - 1 - i, j, pixels[i][(int)slider2.Value][j]);
                }
            }
            BitmapSource bs = BitmapSource.Create(width, countFiles, 96d, 96d, pf, null, bitMap2, stride);
            RenderOptions.SetBitmapScalingMode(bs, BitmapScalingMode.NearestNeighbor);
            CroppedBitmap croppedBitmap = new CroppedBitmap(bs, new Int32Rect(0, 0, width, countFiles));
            var bitmap = new TransformedBitmap(croppedBitmap, 
                new ScaleTransform(1, dicoms[0].spacingBetweenSlices));
            Image2.Source = bitmap;

        }
        public void DrawImage3()
        {
            PixelFormat pf = PixelFormats.Bgr32;
            int stride = (width * pf.BitsPerPixel + 7) / 8;
            bitMap3 = new byte[stride * height];
            FillPixels();
            for (int i = 0; i < countFiles; i++)
            {
                for (int j = 0; j < dicoms[0].rows; j++)
                {
                    SetBitMap(bitMap3, countFiles - 1 - i, j, pixels[i][j][(int)slider3.Value]);
                }
            }

            BitmapSource bs = BitmapSource.Create(width, height, 96d, 96d, pf, null, bitMap3, stride);
            RenderOptions.SetBitmapScalingMode(bs, BitmapScalingMode.NearestNeighbor);
            CroppedBitmap croppedBitmap = new CroppedBitmap(bs, new Int32Rect(0, 0, width, countFiles));
            var bitmap = new TransformedBitmap(croppedBitmap,
                new ScaleTransform(1, dicoms[0].sliceThickness / dicoms[0].pixelSpacing[1]));
            Image3.Source = bitmap;
        }
        public void DrawImages()
        {
            DrawImage1();
            DrawImage2();
            DrawImage3();
        }
        public void FillPixels()
        {
            pixels = new byte[countFiles][][];
            for (int i = 0; i < countFiles; i++)
            {
                pixels[i] = new byte[dicoms[0].rows][];
                for (int j = 0; j < dicoms[0].rows; j++)
                {
                    pixels[i][j] = new byte[dicoms[0].cols];
                    for (int k = 0; k < dicoms[0].rows; k++)
                    {
                        pixels[i][j][k] = dicoms[i].GetColor(j, k);
                    }
                }
            }
        }
        public void SetBitMap(byte[] bitMap, int x, int y, byte color)
        {
            bitMap[512 * 4 * x + 4 * y] = color;
            bitMap[512 * 4 * x + 4 * y + 1] = color;
            bitMap[512 * 4 * x + 4 * y + 2] = color;
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

            IEnumerable<string> files = Directory.EnumerateFiles(directory).OrderBy(f => f);

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                fileBytes = File.ReadAllBytes(file);
                Dicom dicom = new Dicom();
                dicom.name = file;
                dicoms.Add(dicom);
                dicoms[countFiles].LoadFile(fileBytes);
                countFiles += 1;
            }
            
            DrawImages();
            SetSliders();
        }

        public void ClearBitMaps()
        {
            for (int i = 0; i < 512; i++)
            {
                for (int j = 0; j < 512; j++)
                {
                    SetBitMap(bitMap1, i, j, 0);
                    SetBitMap(bitMap2, i, j, 0);
                    SetBitMap(bitMap3, i, j, 0);
                }
            }
        }
    }
}
