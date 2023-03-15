using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Project_DICOM
{
    public class Dicom
    {
        public Dicom() { }

        List<string> specialTags = new List<string>() { "OB", "OW", "OF", "SQ", "UT", "UN" };
        NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;

        // (7fe0, 0010) Pixel Data
        public int[][] pixelData;

        // (0028, 0010) Rows
        public ushort rows;

        // (0028, 0011) Columns
        public ushort cols;

        // (0028, 0101) Bits Stored
        public ushort bitsStored;

        // (0028, 0100) Bits Allocated
        public ushort bitsAllocated;

        // (0028, 1053) Rescale Slope
        public float rescaleSlope;

        // (0028, 1052) Rescale Intercept
        public float rescaleIntercept;

        // (0028, 0030) Pixel Spacing [2]
        public float[] pixelSpacing = new float[2];

        //(0018, 0050) Slice Thickness
        public float sliceThickness;

        // (0020, 0032) Image Position (Patient) [3]
        public float[] imagePosition = new float[3];

        // (0028, 1050) Window Center
        public float windowCenter;

        // (0028, 1051) Window Width
        public float windowWidth;

        // (0020, 1041) Slice Location
        public float sliceLocation;

        public void LoadFile(byte[] bytes)
        {
            bool found = false;
            uint i;

            // Search for DICM
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
                Debug.WriteLine("DICM not found");
                return;
            }

            uint skip = 0;
            uint count = 0;
            ushort tagGroup;
            ushort tagNumber;

            for (i += 4; i < bytes.Length; i += skip)
            {
                skip = 6;
                tagGroup = (ushort)(((0xff & bytes[i + 1]) << 8) + (0xff & bytes[i]));
                tagNumber = (ushort)((bytes[i + 3] << 8) + bytes[i + 2]);

                string dataType = "";
                dataType += (char)bytes[i + 4];
                dataType += (char)bytes[i + 5];

                if (specialTags.Contains(dataType))
                {
                    count = (uint)((bytes[i + 11] << 24) + (bytes[i + 10] << 16) + (bytes[i + 9] << 8) + bytes[i + 8]);
                    skip += 4;
                }
                else
                {
                    count = (uint)((bytes[i + 7] << 8) + bytes[i + 6]);
                }
                skip += 2 + count;

                // (0028, 0010) Rows
                if (tagGroup == 0x0028 && tagNumber == 0x0010)
                {
                    rows = (ushort)((bytes[i + skip - count + 1] << 8) + (bytes[i + skip - count]));
                }

                // (0028, 0011) Columns
                if (tagGroup == 0x0028 && tagNumber == 0x0011)
                {
                    cols = (ushort)((bytes[i + skip - count + 1] << 8) + (bytes[i + skip - count]));
                }

                // (0028, 0101) Bits Stored
                if (tagGroup == 0x0028 && tagNumber == 0x0101)
                {
                    bitsStored = (ushort)((bytes[i + skip - count + 1] << 8) + (bytes[i + skip - count]));
                }

                // (0028, 0100) Bits Allocated
                if (tagGroup == 0x0028 && tagNumber == 0x0100)
                {
                    bitsAllocated = (ushort)((bytes[i + skip - count + 1] << 8) + (bytes[i + skip - count]));
                }
                // (0028, 1053) Rescale Slope
                if (tagGroup == 0x0028 && tagNumber == 0x1053)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    rescaleSlope = a;
                }

                // (0028, 1052) Rescale Intercept
                if (tagGroup == 0x0028 && tagNumber == 0x1052)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    rescaleIntercept = a;
                }

                // (0028, 0030) Pixel Spacing [2]
                if (tagGroup == 0x0028 && tagNumber == 0x0030)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);
                    float b = float.Parse(parts[1], nfi);

                    pixelSpacing[0] = a;
                    pixelSpacing[1] = b;
                }

                //(0018, 0050) Slice Thickness
                if (tagGroup == 0x0018 && tagNumber == 0x0050)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    float a = float.Parse(buffer, nfi);

                    sliceThickness = a;
                }

                // (0020, 0032) Image Position (Patient) [3]
                if (tagGroup == 0x0020 && tagNumber == 0x0032)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');

                    float a = float.Parse(parts[0], nfi);
                    float b = float.Parse(parts[1], nfi);
                    float c = float.Parse(parts[2], nfi);

                    imagePosition[0] = a;
                    imagePosition[1] = b;
                    imagePosition[2] = c;
                }

                // (0028, 1050) Window Center
                if (tagGroup == 0x0028 && tagNumber == 0x1050)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    windowCenter = a;
                }

                // (0028, 1051) Window Width
                if (tagGroup == 0x0028 && tagNumber == 0x1051)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    windowWidth = a;
                }

                // (0020, 1041) Slice Location
                if (tagGroup == 0x0020 && tagNumber == 0x1041)
                {
                    string buffer = CheckPattern(bytes, i + skip - count, i + skip);
                    var parts = buffer.Split('\\');
                    float a = float.Parse(parts[0], nfi);

                    sliceLocation = a;
                }

                // (7fe0, 0010) Pixel Data
                if (tagGroup == 0x7fe0 && tagNumber == 0x0010)
                {
                    i += skip - count;
                    break;
                }
            }

            // Read Pixels
            pixelData = new int[rows][];
            for (uint k = 0; k < rows; k++)
            {
                pixelData[k] = new int[cols];
            }
            uint row = 0, col = 0;
            for (; i < bytes.Length; i += 2)
            {
                pixelData[row][col] = ((0xff & bytes[i + 1]) << 8) + (0xff & bytes[i]);
                col++;
                if (col == cols)
                {
                    col = 0;
                    row++;
                }
            }
        }
        public byte GetColor(int x, int y, string type)
        {
            float value = pixelData[x][y] * rescaleSlope + rescaleIntercept;
            byte yMin = 0;
            byte yMax = 255;
            float center = windowCenter - 0.5f;
            float width = windowWidth - 1.0f;
            if (type == "bone")
            {
                // 300-400 i 500-1900
                center = 300 - 0.5f;
                width = 1900 - 1.0f;
            }
            if (type == "brain")
            {
                // 40-80
                center = 40 - 0.5f;
                width = 80 - 1.0f;
            }
            if (type == "angio")
            {
                // 300-600
                center = 300 - 0.5f;
                width = 600 - 1.0f;
            }

            if (value <= (center - width / 2.0f))
            {
                return yMin;
            }
            else if (value > (center + width / 2.0f))
            {
                return yMax;
            }
            else
            {
                return (byte)(((value - center) / width + 0.5f) * (yMax - yMin) + yMin);
            }
        }
        public string CheckPattern(byte[] bytes, uint start, uint end)
        {
            char[] buffer = new char[(ulong)(end - start + 1)];
            int i = 0;
            for (; bytes[start] != bytes[end]; start++)
            {
                buffer[i] = (char)bytes[start];
                i++;
            }
            buffer[i] = '\0';
            return new string(buffer);
        }

    }
}
