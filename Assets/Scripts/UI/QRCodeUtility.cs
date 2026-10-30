using System;
using System.Collections.Generic;
using UnityEngine;

namespace GyroPilot.UI
{
    /// <summary>
    /// Generador autnomo de cdigos QR en C# puro para Unity (sin dependencias externas ni internet).
    /// Soporta codificacin de URLs locales para conexin plug-and-play con el celular.
    /// </summary>
    public static class QRCodeUtility
    {
        public static Texture2D GenerateQRCodeTexture(string text, int scale = 6)
        {
            bool[,] matrix = GenerateMatrix(text);
            int width = matrix.GetLength(0);
            int height = matrix.GetLength(1);

            int texWidth = (width + 4) * scale;
            int texHeight = (height + 4) * scale;

            Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color[] colors = new Color[texWidth * texHeight];
            Color white = Color.white;
            Color black = Color.black;

            // Rellenar fondo blanco
            for (int i = 0; i < colors.Length; i++) colors[i] = white;

            // Dibujar mdulos con borde de 2 mdulos de quiet zone
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (matrix[x, y])
                    {
                        int startX = (x + 2) * scale;
                        int startY = (height - 1 - y + 2) * scale;

                        for (int dy = 0; dy < scale; dy++)
                        {
                            for (int dx = 0; dx < scale; dx++)
                            {
                                colors[(startY + dy) * texWidth + (startX + dx)] = black;
                            }
                        }
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        private static bool[,] GenerateMatrix(string text)
        {
            // Versin 3 (29x29) o Versin 4 (33x33) soporta URLs de 40-50 caracteres fcilmente
            int size = 33;
            bool[,] matrix = new bool[size, size];
            bool[,] isReserved = new bool[size, size];

            // 1. Finder patterns (esquinas 7x7)
            AddFinderPattern(matrix, isReserved, 0, 0);
            AddFinderPattern(matrix, isReserved, size - 7, 0);
            AddFinderPattern(matrix, isReserved, 0, size - 7);

            // 2. Alignment pattern en versin 4 (centro en 24, 24)
            AddAlignmentPattern(matrix, isReserved, 24, 24);

            // 3. Timing patterns (lneas alternadas en x=6 y y=6)
            for (int i = 8; i < size - 8; i++)
            {
                matrix[6, i] = (i % 2 == 0);
                isReserved[6, i] = true;
                matrix[i, 6] = (i % 2 == 0);
                isReserved[i, 6] = true;
            }

            // 4. Reservar zonas de formato
            for (int i = 0; i < 9; i++)
            {
                isReserved[8, i] = true;
                isReserved[i, 8] = true;
                if (size - 1 - i >= 0)
                {
                    isReserved[8, size - 1 - i] = true;
                    isReserved[size - 1 - i, 8] = true;
                }
            }
            matrix[8, size - 8] = true; // Dark module

            // 5. Convertir texto a bytes con modo Byte (0100) y relleno
            byte[] textBytes = System.Text.Encoding.ASCII.GetBytes(text);
            List<byte> bitData = new List<byte>();

            // Modo Byte: 0100
            AppendBits(bitData, 4, 4);
            // Longitud de caracteres (8 bits para versin 1-9)
            AppendBits(bitData, textBytes.Length, 8);
            // Caracteres
            foreach (byte b in textBytes)
            {
                AppendBits(bitData, b, 8);
            }
            // Terminador
            AppendBits(bitData, 0, 4);

            // Rellenar a mltiplo de 8 bits
            while (bitData.Count % 8 != 0) bitData.Add(0);

            // Rellenar codewords con 11101100 y 00010001
            byte[] pad = new byte[] { 0xEC, 0x11 };
            int padIdx = 0;
            while (bitData.Count < 500) // codewords suficientes para versin 4
            {
                AppendBits(bitData, pad[padIdx % 2], 8);
                padIdx++;
            }

            // 6. Colocar datos en la matriz en patrn zigzag de derecha a izquierda
            int bitIdx = 0;
            int right = size - 1;
            bool goingUp = true;

            while (right > 0)
            {
                if (right == 6) right--; // Saltar lnea de timing vertical

                for (int vertical = 0; vertical < size; vertical++)
                {
                    int y = goingUp ? (size - 1 - vertical) : vertical;

                    for (int dx = 0; dx < 2; dx++)
                    {
                        int x = right - dx;
                        if (!isReserved[x, y])
                        {
                            bool bit = bitIdx < bitData.Count && bitData[bitIdx] == 1;
                            // Aplicar mscara 000: (x + y) % 2 == 0
                            bool mask = (x + y) % 2 == 0;
                            matrix[x, y] = bit ^ mask;
                            bitIdx++;
                        }
                    }
                }
                right -= 2;
                goingUp = !goingUp;
            }

            // 7. Informacin de formato (Mscara 000 + Nivel L con correccin BCH)
            // Formato precalculado para Nivel L, Mscara 0: 0x77C4 (111011111000100 binario)
            int formatBits = 0x77C4;
            for (int i = 0; i < 15; i++)
            {
                bool b = ((formatBits >> i) & 1) == 1;
                // Alrededor de esquina superior izquierda
                if (i <= 5) matrix[8, i] = b;
                else if (i == 6) matrix[8, 7] = b;
                else if (i == 7) matrix[8, 8] = b;
                else if (i == 8) matrix[7, 8] = b;
                else matrix[14 - i, 8] = b;

                // Esquinas restantes
                if (i < 8) matrix[size - 1 - i, 8] = b;
                else matrix[8, size - 15 + i] = b;
            }

            return matrix;
        }

        private static void AddFinderPattern(bool[,] matrix, bool[,] isReserved, int startX, int startY)
        {
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    bool isBlack = (x == 0 || x == 6 || y == 0 || y == 6) || (x >= 2 && x <= 4 && y >= 2 && y <= 4);
                    matrix[startX + x, startY + y] = isBlack;
                    isReserved[startX + x, startY + y] = true;
                }
            }

            // Separador blanco alrededor
            for (int y = -1; y <= 7; y++)
            {
                for (int x = -1; x <= 7; x++)
                {
                    int px = startX + x;
                    int py = startY + y;
                    if (px >= 0 && px < matrix.GetLength(0) && py >= 0 && py < matrix.GetLength(1))
                    {
                        isReserved[px, py] = true;
                    }
                }
            }
        }

        private static void AddAlignmentPattern(bool[,] matrix, bool[,] isReserved, int centerX, int centerY)
        {
            for (int y = -2; y <= 2; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    int px = centerX + x;
                    int py = centerY + y;
                    bool isBlack = (Math.Abs(x) == 2 || Math.Abs(y) == 2) || (x == 0 && y == 0);
                    matrix[px, py] = isBlack;
                    isReserved[px, py] = true;
                }
            }
        }

        private static void AppendBits(List<byte> list, int value, int bitCount)
        {
            for (int i = bitCount - 1; i >= 0; i--)
            {
                list.Add((byte)((value >> i) & 1));
            }
        }
    }
}
