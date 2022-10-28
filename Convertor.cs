using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemPlus;
using SystemPlus.Utils;

namespace TextVideo
{
    public static class Convertor
    {
        private static byte nl = (byte)'\n';

        public static readonly char[] brightnesString = new char[] {'$', '@', 'B', '%', '8', '&', 'W', 'M', '#', '*', 'o', 'a', 'h', 'k', 'b', 'd', 'p', 'q', 'w', 'm', 
            'Z', 'O', '0', 'Q', 'L', 'C', 'J', 'U', 'Y', 'X', 'z', 'c', 'v', 'u', 'n', 'x', 'r', 'j', 'f', 't', '/', '\\', '|', '(', ')', '1', '{', '}', '[', ']', '?', 
            '-', '_', '+', '~', '<', '>', 'i', '!', 'l', 'I', ';', ':', ',', '\"', '^', '`', '\'', '.', ' ' };

        public static char[] brightnesStringInv = new char[] {' ', '.', '\'', '`', '^', '\"', ',', ':', ';', 'I', 'l', '!', 'i', '>', '<', '~', '_', '-', '?', ']',
            '[', '}', '{', '1', ')', '(', '|', '\\', '/', 't', 'f', 'j', 'r', 'x', 'n', 'u', 'v', 'c', 'z', 'X', 'Y', 'U', 'J', 'C', 'L', 'Q', '0', 'O', 'Z', 'm', 'w',
            'q', 'p', 'd', 'b', 'k', 'h', 'a', 'o', '*', '#', 'M', 'W', '&', '8', '%', 'B', '@', '$' };

        public static char[] brightnesString2 = new char[] { '█', '▓', '▒', '░', ' ' };

        public static char[] brightnesString2Inv = new char[] { ' ', '░', '▒', '▓', '█' };

        public static char[] bsc = brightnesString;

        private static byte[] brightnessBytes;

        public static void Init()
        {
            brightnessBytes = new byte[bsc.Length];
            
            for (int i = 0; i < bsc.Length; i++)
                brightnessBytes[i] = (byte)bsc[i];
        }

        public static char[] Convert(DirectBitmap img, int outWidth, int outHeight, bool squarePixels = true)
        {
            int width = squarePixels ? (outWidth * 2 + 1) : (outWidth + 1);
            int height = outHeight;

            char[] asciiImg = new char[width * height];

            float numbBrightLevels = (float)bsc.Length - 1;

            float pw = (float)img.Width / (float)outWidth;
            float ph = (float)img.Height / (float)outHeight;

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < outWidth; x++) {
                    int _x = MathPlus.RoundToInt((float)x * pw);
                    int _y = MathPlus.RoundToInt((float)y * ph);

                    if (_x > img.Width - 1)
                        _x = img.Width - 1;
                    if (_y > img.Height - 1)
                        _y = img.Height - 1;

                    Color c = img.GetPixel(_x, _y);
                    int brightnes = MathPlus.RoundToInt(((float)((c.R + c.G + c.B) / 3) / 255f) * numbBrightLevels);
                    if (brightnes < 0)
                        brightnes = 0;
                    else if (brightnes >= bsc.Length)
                        brightnes = bsc.Length - 1;
                    

                    char ch = bsc[brightnes];

                    if (squarePixels) {
                        asciiImg[y * width + x * 2] = ch;
                        asciiImg[y * width + x * 2 + 1] = ch;
                    }
                    else {
                        asciiImg[y * width + x] = ch;
                    }
                }

                asciiImg[y * width + width - 1] = '\n';
            }

            return asciiImg;
        }

        public static byte[] ConvertToBytes(DirectBitmap img, int outWidth, int outHeight, bool squarePixels = true)
        {
            int width = squarePixels ? (outWidth * 2 + 1) : (outWidth + 1);
            int height = outHeight;

            byte[] asciiImg = new byte[width * height];

            float numbBrightLevels = (float)brightnesString.Length - 1;

            float pw = (float)img.Width / (float)outWidth;
            float ph = (float)img.Height / (float)outHeight;

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < outWidth; x++) {
                    int _x = MathPlus.RoundToInt((float)x * pw);
                    int _y = MathPlus.RoundToInt((float)y * ph);

                    if (_x > img.Width - 1)
                        _x = img.Width - 1;
                    if (_y > img.Height - 1)
                        _y = img.Height - 1;

                    Color c = img.GetPixel(_x, _y);
                    int brightnes = MathPlus.RoundToInt(((float)((c.R + c.G + c.B) / 3) / 255f) * numbBrightLevels);
                    if (brightnes < 0)
                        brightnes = 0;
                    else if (brightnes >= brightnessBytes.Length)
                        brightnes = brightnessBytes.Length - 1;

                    if (squarePixels) {
                        byte b = brightnessBytes[brightnes];
                        asciiImg[y * width + x * 2] = b;
                        asciiImg[y * width + x * 2 + 1] = b;
                    }
                    else {
                        asciiImg[y * width + x] = brightnessBytes[brightnes];
                    }
                }

                asciiImg[y * width + width - 1] = nl;
            }

            return asciiImg;
        }

        public static string Inverse(string img, string brightnesString)
        {
            char[] outImg = new char[img.Length];

            for (int i = 0; i < img.Length; i++)
            {
                int index = -1;

                for (int j = 0; j < brightnesString.Length; j++)
                    if (img[i] == brightnesString[j])
                    {
                        index = j;
                        break;
                    }

                if (index == -1)
                    outImg[i] = img[i];
                else
                {
                    if (brightnesString.Length % 2 == 1)
                    {
                        int center = MathPlus.FloorToInt(brightnesString.Length / 2f);

                        if (index == center)
                            outImg[i] = brightnesString[index];
                        else
                            outImg[i] = brightnesString[-(index - center) + center];
                    } else
                    {
                        int center = brightnesString.Length / 2;
                        outImg[i] = brightnesString[-(index - center) + center - 1];
                    }
                }
            }
            return new string(outImg);
        }
    }
}
