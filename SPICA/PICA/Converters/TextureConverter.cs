using SPICA.PICA.Commands;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SPICA.PICA.Converters
{
    static class TextureConverter
    {
        private static int[] FmtBPP = new int[] { 32, 24, 16, 16, 16, 16, 16, 8, 8, 8, 4, 4, 4, 8 };

        private static int[] SwizzleLUT =
        {
             0,  1,  8,  9,  2,  3, 10, 11,
            16, 17, 24, 25, 18, 19, 26, 27,
             4,  5, 12, 13,  6,  7, 14, 15,
            20, 21, 28, 29, 22, 23, 30, 31,
            32, 33, 40, 41, 34, 35, 42, 43,
            48, 49, 56, 57, 50, 51, 58, 59,
            36, 37, 44, 45, 38, 39, 46, 47,
            52, 53, 60, 61, 54, 55, 62, 63
        };

        public static byte[] DecodeBuffer(byte[] Input, int Width, int Height, PICATextureFormat Format)
        {
            if (Format == PICATextureFormat.ETC1 ||
                Format == PICATextureFormat.ETC1A4)
            {
                return TextureCompression.ETC1Decompress(Input, Width, Height, Format == PICATextureFormat.ETC1A4);
            }
            else
            {
                int Increment = FmtBPP[(int)Format] / 8;

                if (Increment == 0) Increment = 1;

                byte[] Output = new byte[Width * Height * 4];

                int IOffs = 0;

                for (int TY = 0; TY < Height; TY += 8)
                {
                    for (int TX = 0; TX < Width; TX += 8)
                    {
                        for (int Px = 0; Px < 64; Px++)
                        {
                            int X = SwizzleLUT[Px] & 7;
                            int Y = (SwizzleLUT[Px] - X) >> 3;

                            int OOffs = (TX + X + ((Height - 1 - (TY + Y)) * Width)) * 4;

                            switch (Format)
                            {
                                case PICATextureFormat.RGBA8:
                                    Output[OOffs + 0] = Input[IOffs + 3];
                                    Output[OOffs + 1] = Input[IOffs + 2];
                                    Output[OOffs + 2] = Input[IOffs + 1];
                                    Output[OOffs + 3] = Input[IOffs + 0];

                                    break;

                                case PICATextureFormat.RGB8:
                                    Output[OOffs + 0] = Input[IOffs + 2];
                                    Output[OOffs + 1] = Input[IOffs + 1];
                                    Output[OOffs + 2] = Input[IOffs + 0];
                                    Output[OOffs + 3] = 0xff;

                                    break;

                                case PICATextureFormat.RGBA5551:
                                    DecodeRGBA5551(Output, OOffs, GetUShort(Input, IOffs));

                                    break;

                                case PICATextureFormat.RGB565:
                                    DecodeRGB565(Output, OOffs, GetUShort(Input, IOffs));

                                    break;

                                case PICATextureFormat.RGBA4:
                                    DecodeRGBA4(Output, OOffs, GetUShort(Input, IOffs));

                                    break;

                                case PICATextureFormat.LA8:
                                    Output[OOffs + 0] = Input[IOffs + 1];
                                    Output[OOffs + 1] = Input[IOffs + 1];
                                    Output[OOffs + 2] = Input[IOffs + 1];
                                    Output[OOffs + 3] = Input[IOffs + 0];

                                    break;

                                case PICATextureFormat.HiLo8:
                                    Output[OOffs + 0] = Input[IOffs + 1];
                                    Output[OOffs + 1] = Input[IOffs + 0];
                                    Output[OOffs + 2] = 0;
                                    Output[OOffs + 3] = 0xff;

                                    break;

                                case PICATextureFormat.L8:
                                    Output[OOffs + 0] = Input[IOffs];
                                    Output[OOffs + 1] = Input[IOffs];
                                    Output[OOffs + 2] = Input[IOffs];
                                    Output[OOffs + 3] = 0xff;

                                    break;

                                case PICATextureFormat.A8:
                                    Output[OOffs + 0] = 0xff;
                                    Output[OOffs + 1] = 0xff;
                                    Output[OOffs + 2] = 0xff;
                                    Output[OOffs + 3] = Input[IOffs];

                                    break;

                                case PICATextureFormat.LA4:
                                    Output[OOffs + 0] = (byte)((Input[IOffs] >> 4) | (Input[IOffs] & 0xf0));
                                    Output[OOffs + 1] = (byte)((Input[IOffs] >> 4) | (Input[IOffs] & 0xf0));
                                    Output[OOffs + 2] = (byte)((Input[IOffs] >> 4) | (Input[IOffs] & 0xf0));
                                    Output[OOffs + 3] = (byte)((Input[IOffs] << 4) | (Input[IOffs] & 0x0f));

                                    break;

                                case PICATextureFormat.L4:
                                    int L = (Input[IOffs >> 1] >> ((IOffs & 1) << 2)) & 0xf;

                                    Output[OOffs + 0] = (byte)((L << 4) | L);
                                    Output[OOffs + 1] = (byte)((L << 4) | L);
                                    Output[OOffs + 2] = (byte)((L << 4) | L);
                                    Output[OOffs + 3] = 0xff;

                                    break;

                                case PICATextureFormat.A4:
                                    int A = (Input[IOffs >> 1] >> ((IOffs & 1) << 2)) & 0xf;

                                    Output[OOffs + 0] = 0xff;
                                    Output[OOffs + 1] = 0xff;
                                    Output[OOffs + 2] = 0xff;
                                    Output[OOffs + 3] = (byte)((A << 4) | A);

                                    break;
                            }

                            IOffs += Increment;
                        }
                    }
                }

                return Output;
            }
        }

        private static void DecodeRGBA5551(byte[] Buffer, int Address, ushort Value)
        {
            int R = ((Value >> 1) & 0x1f) << 3;
            int G = ((Value >> 6) & 0x1f) << 3;
            int B = ((Value >> 11) & 0x1f) << 3;

            SetColor(Buffer, Address, (Value & 1) * 0xff,
                B | (B >> 5),
                G | (G >> 5),
                R | (R >> 5));
        }

        private static void EncodeRGBA5551(byte[] Input, int InputAddress, byte[] Output, int OutputAddress)
        {
            uint RGBA = GetUInt(Input, InputAddress);

#pragma warning disable CS0675 // Opérateur de bits or utilisé sur un opérande de signe étendu ; effectuez un cast en type plus faible non signé
            byte ARG = (byte)((((RGBA & 0xFF000000) > 0) ? 1 : 0) | (RGBA >> 2 & 0x3E) | (RGBA >> 5 & 0xC0));
#pragma warning restore CS0675 // Opérateur de bits or utilisé sur un opérande de signe étendu ; effectuez un cast en type plus faible non signé
            byte GB = (byte)((RGBA >> 13 & 0x7) | (RGBA >> 16 & 0xF8));

            Output[OutputAddress] = ARG;
            Output[OutputAddress + 1] = GB;
        }

        private static void DecodeRGB565(byte[] Buffer, int Address, ushort Value)
        {
            int R = ((Value >> 0) & 0x1f) << 3;
            int G = ((Value >> 5) & 0x3f) << 2;
            int B = ((Value >> 11) & 0x1f) << 3;

            SetColor(Buffer, Address, 0xff,
                B | (B >> 5),
                G | (G >> 6),
                R | (R >> 5));
        }

        private static void EncodeRGB565(byte[] Input, int InputAddress, byte[] Output, int OutputAddress)
        {
            uint RGB = GetUInt(Input, InputAddress);

            byte RG = (byte)((RGB >> 3 & 0x1f) | (RGB >> 5 & 0xE0));
            byte GB = (byte)((RGB >> 13 & 0x7) | (RGB >> 16 & 0xF8));

            Output[OutputAddress] = RG;
            Output[OutputAddress + 1] = GB;
        }

        private static void DecodeRGBA4(byte[] Buffer, int Address, ushort Value)
        {
            int R = (Value >> 4) & 0xf;
            int G = (Value >> 8) & 0xf;
            int B = (Value >> 12) & 0xf;

            SetColor(Buffer, Address, (Value & 0xf) | (Value << 4),
                B | (B << 4),
                G | (G << 4),
                R | (R << 4));
        }

        private static void EncodeRGBA4(byte[] Input, int InputAddress, byte[] Output, int OutputAddress)
        {
            byte GB = (byte)((Input[InputAddress + 2] & 0xF0) | (Input[InputAddress + 1] >> 4 & 0x0F));
            byte AR = (byte)((Input[InputAddress] & 0xF0) | (Input[InputAddress + 3] >> 4 & 0x0F));

            Output[OutputAddress] = AR;
            Output[OutputAddress + 1] = GB;
        }

        private static void SetColor(byte[] Buffer, int Address, int A, int B, int G, int R)
        {
            Buffer[Address + 0] = (byte)B;
            Buffer[Address + 1] = (byte)G;
            Buffer[Address + 2] = (byte)R;
            Buffer[Address + 3] = (byte)A;
        }

        private static ushort GetUShort(byte[] Buffer, int Address)
        {
            return (ushort)(
                Buffer[Address + 0] << 0 |
                Buffer[Address + 1] << 8);
        }

        private static uint GetUInt(byte[] Buffer, int Address)
        {
            return (uint)(
                Buffer[Address + 0] << 0 |
                Buffer[Address + 1] << 8 |
                Buffer[Address + 2] << 16 |
                Buffer[Address + 3] << 24
            );
        }

        public static Bitmap DecodeBitmap(byte[] Input, int Width, int Height, PICATextureFormat Format)
        {
            byte[] Buffer = DecodeBuffer(Input, Width, Height, Format);

            byte[] Output = new byte[Buffer.Length];

            int Stride = Width * 4;

            for (int Y = 0; Y < Height; Y++)
            {
                int IOffs = Stride * Y;
                int OOffs = Stride * (Height - 1 - Y);

                for (int X = 0; X < Width; X++)
                {
                    Output[OOffs + 0] = Buffer[IOffs + 2];
                    Output[OOffs + 1] = Buffer[IOffs + 1];
                    Output[OOffs + 2] = Buffer[IOffs + 0];
                    Output[OOffs + 3] = Buffer[IOffs + 3];

                    IOffs += 4;
                    OOffs += 4;
                }
            }

            return GetBitmap(Output, Width, Height);
        }

        public static byte[] Encode(Bitmap Img, PICATextureFormat Format)
        {
            byte[] Input = GetBuffer(Img);

            byte[] Output = new byte[CalculateLength(Img.Width, Img.Height, Format)];

            int BPP = FmtBPP[(int)Format] / 8;
            if (BPP == 0)
            {
                BPP = 1;
            }

            int OOffs = 0;

            if (Format == PICATextureFormat.ETC1 ||
                Format == PICATextureFormat.ETC1A4)
            {
                return ETC1_Encode(Input, Img.Width, Img.Height, Format);
            }
            else
            {
                for (int TY = 0; TY < Img.Height; TY += 8)
                {
                    for (int TX = 0; TX < Img.Width; TX += 8)
                    {
                        for (int Px = 0; Px < 64; Px++)
                        {
                            int X = SwizzleLUT[Px] & 7;
                            int Y = (SwizzleLUT[Px] - X) >> 3;

                            int IOffs = (TX + X + ((TY + Y) * Img.Width)) * 4;

                            switch (Format)
                            {
                                case PICATextureFormat.RGB8:
                                    Array.Copy(Input, IOffs, Output, OOffs, 3);

                                    break;
                                case PICATextureFormat.RGBA8:
                                    Output[OOffs + 0] = Input[IOffs + 3];
                                    Output[OOffs + 1] = Input[IOffs + 0];
                                    Output[OOffs + 2] = Input[IOffs + 1];
                                    Output[OOffs + 3] = Input[IOffs + 2];

                                    break;
                                case PICATextureFormat.RGB565:
                                    EncodeRGB565(Input, IOffs, Output, OOffs);

                                    break;
                                case PICATextureFormat.L8:
                                    Output[OOffs] = GetLuminosity(Input, IOffs);

                                    break;
                                case PICATextureFormat.A8:
                                    Output[OOffs] = Input[IOffs + 3];

                                    break;
                                case PICATextureFormat.LA8:
                                    Output[OOffs] = GetLuminosity(Input, IOffs);
                                    Output[OOffs + 1] = Input[IOffs + 3];

                                    break;
                                case PICATextureFormat.HiLo8:
                                    Array.Copy(Input, IOffs, Output, OOffs, 2);
                                    break;
                                case PICATextureFormat.RGBA4:
                                    EncodeRGBA4(Input, IOffs, Output, OOffs);

                                    break;
                                case PICATextureFormat.RGBA5551:
                                    EncodeRGBA5551(Input, IOffs, Output, OOffs);

                                    break;
                                case PICATextureFormat.L4:
                                    {
                                        int ActualOOffs = OOffs / 2;
                                        int Shift = (OOffs & 1) * 4;
                                        Output[ActualOOffs] |= (byte)((GetLuminosity(Input, IOffs) >> 4 & 0xF) << Shift);
                                    }
                                    break;
                                case PICATextureFormat.A4:
                                    {
                                        int ActualOOffs = OOffs / 2;
                                        int Shift = (OOffs & 1) * 4;
                                        Output[ActualOOffs] |= (byte)((Input[IOffs + 3] >> 4 & 0xF) << Shift);
                                    }
                                    break;
                                case PICATextureFormat.LA4:
                                    Output[OOffs] = (byte)(GetLuminosity(Input, IOffs) & 0xF0 | (Input[IOffs + 3] >> 4 & 0xf));
                                    break;

                                default: throw new NotImplementedException();
                            }

                            OOffs += BPP;
                        }
                    }
                }
            }

            return Output;
        }

        private static int[,] Modulation_Table = new[,] {
            { 2, 8, -2, -8 },
            { 5, 17, -5, -17 },
            { 9, 29, -9, -29 },
            { 13, 42, -13, -42 },
            { 18, 60, -18, -60 },
            { 24, 80, -24, -80 },
            { 33, 106, -33, -106 },
            { 47, 183, -47, -183 }
        };

        public static byte GetLuminosity(byte[] RGBA, int IOffs)
        {
            return (byte)((RGBA[IOffs] + RGBA[IOffs + 1] + RGBA[IOffs + 2]) / 3);
        }

        public static byte[] ETC1_Encode(byte[] Data, int Width, int Height, PICATextureFormat format)
        {
            byte[] Out_Data = null;

            // Os tiles com compressão ETC1 no 3DS estão embaralhados
            byte[] Out = new byte[(Width * Height * 4)];
            int[] Tile_Scramble = Get_ETC1_Scramble(Width, Height);

            int i = 0;
            for (int Tile_Y = 0; Tile_Y <= (Height / 4) - 1; Tile_Y++)
            {
                for (int Tile_X = 0; Tile_X <= (Width / 4) - 1; Tile_X++)
                {
                    int TX = Tile_Scramble[i] % (Width / 4);
                    int TY = (Tile_Scramble[i] - TX) / (Width / 4);
                    for (int Y = 0; Y <= 3; Y++)
                    {
                        for (int X = 0; X <= 3; X++)
                        {
                            int Out_Offset = ((TX * 4) + X + ((((TY * 4) + Y)) * Width)) * 4;
                            int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;

                            Out[Out_Offset] = Data[Image_Offset + 2];
                            Out[Out_Offset + 1] = Data[Image_Offset + 1];
                            Out[Out_Offset + 2] = Data[Image_Offset];
                            if (format == PICATextureFormat.ETC1A4)
                                Out[Out_Offset + 3] = Data[Image_Offset + 3];
                            else
                                Out[Out_Offset + 3] = 0xFF;
                        }
                    }
                    i += 1;
                }
            }

            Out_Data = new byte[((Width * Height) / (format == PICATextureFormat.ETC1 ? 2 : 1))];
            int Out_Data_Offset = 0;

            for (int Tile_Y = 0; Tile_Y <= (Height / 4) - 1; Tile_Y++)
            {
                for (int Tile_X = 0; Tile_X <= (Width / 4) - 1; Tile_X++)
                {
                    bool Flip = false;
                    bool Difference = false;
                    int Block_Top = 0;
                    int Block_Bottom = 0;

                    // Teste do Difference Bit
                    int Diff_Match_V = 0;
                    int Diff_Match_H = 0;
                    for (int Y = 0; Y <= 3; Y++)
                    {
                        for (int X = 0; X <= 1; X++)
                        {
                            int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Image_Offset_2 = ((Tile_X * 4) + (2 + X) + (((Tile_Y * 4) + Y) * Width)) * 4;

                            byte Bits_R1 = Convert.ToByte(Out[Image_Offset_1] & 0xF8);
                            byte Bits_G1 = Convert.ToByte(Out[Image_Offset_1 + 1] & 0xF8);
                            byte Bits_B1 = Convert.ToByte(Out[Image_Offset_1 + 2] & 0xF8);

                            byte Bits_R2 = Convert.ToByte(Out[Image_Offset_2] & 0xF8);
                            byte Bits_G2 = Convert.ToByte(Out[Image_Offset_2 + 1] & 0xF8);
                            byte Bits_B2 = Convert.ToByte(Out[Image_Offset_2 + 2] & 0xF8);

                            if ((Bits_R1 == Bits_R2) & (Bits_G1 == Bits_G2) & (Bits_B1 == Bits_B2))
                                Diff_Match_V += 1;
                        }
                    }
                    for (int Y = 0; Y <= 1; Y++)
                    {
                        for (int X = 0; X <= 3; X++)
                        {
                            int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Image_Offset_2 = ((Tile_X * 4) + X + (((Tile_Y * 4) + (2 + Y)) * Width)) * 4;

                            byte Bits_R1 = Convert.ToByte(Out[Image_Offset_1] & 0xF8);
                            byte Bits_G1 = Convert.ToByte(Out[Image_Offset_1 + 1] & 0xF8);
                            byte Bits_B1 = Convert.ToByte(Out[Image_Offset_1 + 2] & 0xF8);

                            byte Bits_R2 = Convert.ToByte(Out[Image_Offset_2] & 0xF8);
                            byte Bits_G2 = Convert.ToByte(Out[Image_Offset_2 + 1] & 0xF8);
                            byte Bits_B2 = Convert.ToByte(Out[Image_Offset_2 + 2] & 0xF8);

                            if ((Bits_R1 == Bits_R2) & (Bits_G1 == Bits_G2) & (Bits_B1 == Bits_B2))
                                Diff_Match_H += 1;
                        }
                    }
                    if (Diff_Match_H == 8)
                    {
                        Difference = true;
                        Flip = true;
                    }
                    else if (Diff_Match_V == 8)
                        Difference = true;
                    else
                    {
                        int Test_R1 = 0;
                        int Test_G1 = 0;
                        int Test_B1 = 0;
                        int Test_R2 = 0;
                        int Test_G2 = 0;
                        int Test_B2 = 0;
                        for (int Y = 0; Y <= 1; Y++)
                        {
                            for (int X = 0; X <= 1; X++)
                            {
                                int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                int Image_Offset_2 = ((Tile_X * 4) + (2 + X) + (((Tile_Y * 4) + (2 + Y)) * Width)) * 4;

                                Test_R1 += Out[Image_Offset_1];
                                Test_G1 += Out[Image_Offset_1 + 1];
                                Test_B1 += Out[Image_Offset_1 + 2];

                                Test_R2 += Out[Image_Offset_2];
                                Test_G2 += Out[Image_Offset_2 + 1];
                                Test_B2 += Out[Image_Offset_2 + 2];
                            }
                        }

                        Test_R1 /= 8;
                        Test_G1 /= 8;
                        Test_B1 /= 8;

                        Test_R2 /= 8;
                        Test_G2 /= 8;
                        Test_B2 /= 8;

                        int Test_Luma_1 = Convert.ToInt32(0.299F * Test_R1 + 0.587F * Test_G1 + 0.114F * Test_B1);
                        int Test_Luma_2 = Convert.ToInt32(0.299F * Test_R2 + 0.587F * Test_G2 + 0.114F * Test_B2);
                        int Test_Flip_Diff = Math.Abs(Test_Luma_1 - Test_Luma_2);
                        if (Test_Flip_Diff > 48)
                            Flip = true;
                    }

                    int Avg_R1 = 0;
                    int Avg_G1 = 0;
                    int Avg_B1 = 0;
                    int Avg_R2 = 0;
                    int Avg_G2 = 0;
                    int Avg_B2 = 0;

                    // Primeiro, cálcula a média de cores de cada bloco
                    if (Flip)
                    {
                        for (int Y = 0; Y <= 1; Y++)
                        {
                            for (int X = 0; X <= 3; X++)
                            {
                                int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                int Image_Offset_2 = ((Tile_X * 4) + X + (((Tile_Y * 4) + (2 + Y)) * Width)) * 4;

                                Avg_R1 += Out[Image_Offset_1];
                                Avg_G1 += Out[Image_Offset_1 + 1];
                                Avg_B1 += Out[Image_Offset_1 + 2];

                                Avg_R2 += Out[Image_Offset_2];
                                Avg_G2 += Out[Image_Offset_2 + 1];
                                Avg_B2 += Out[Image_Offset_2 + 2];
                            }
                        }
                    }
                    else
                        for (int Y = 0; Y <= 3; Y++)
                        {
                            for (int X = 0; X <= 1; X++)
                            {
                                int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                int Image_Offset_2 = ((Tile_X * 4) + (2 + X) + (((Tile_Y * 4) + Y) * Width)) * 4;

                                Avg_R1 += Out[Image_Offset_1];
                                Avg_G1 += Out[Image_Offset_1 + 1];
                                Avg_B1 += Out[Image_Offset_1 + 2];

                                Avg_R2 += Out[Image_Offset_2];
                                Avg_G2 += Out[Image_Offset_2 + 1];
                                Avg_B2 += Out[Image_Offset_2 + 2];
                            }
                        }

                    Avg_R1 /= 8;
                    Avg_G1 /= 8;
                    Avg_B1 /= 8;

                    Avg_R2 /= 8;
                    Avg_G2 /= 8;
                    Avg_B2 /= 8;

                    if (Difference)
                    {
                        // +============+
                        // | Difference |
                        // +============+
                        if ((Avg_R1 & 7) > 3)
                        {
                            Avg_R1 = Clip(Avg_R1 + 8); Avg_R2 = Clip(Avg_R2 + 8);
                        }
                        if ((Avg_G1 & 7) > 3)
                        {
                            Avg_G1 = Clip(Avg_G1 + 8); Avg_G2 = Clip(Avg_G2 + 8);
                        }
                        if ((Avg_B1 & 7) > 3)
                        {
                            Avg_B1 = Clip(Avg_B1 + 8); Avg_B2 = Clip(Avg_B2 + 8);
                        }

                        Block_Top = (Avg_R1 & 0xF8) | (((Avg_R2 - Avg_R1) / 8) & 7);
                        Block_Top = Block_Top | (((Avg_G1 & 0xF8) << 8) | ((((Avg_G2 - Avg_G1) / 8) & 7) << 8));
                        Block_Top = Block_Top | (((Avg_B1 & 0xF8) << 16) | ((((Avg_B2 - Avg_B1) / 8) & 7) << 16));

                        // Vamos ter certeza de que os mesmos valores obtidos pelo descompressor serão usados na comparação (modo Difference)
                        Avg_R1 = Block_Top & 0xF8;
                        Avg_G1 = (Block_Top & 0xF800) >> 8;
                        Avg_B1 = (Block_Top & 0xF80000) >> 16;

                        int R = Signed_Byte(Convert.ToByte(Avg_R1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 7) << 5)) >> 5);
                        int G = Signed_Byte(Convert.ToByte(Avg_G1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 0x700) >> 3)) >> 5);
                        int B = Signed_Byte(Convert.ToByte(Avg_B1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 0x70000) >> 11)) >> 5);

                        Avg_R2 = R;
                        Avg_G2 = G;
                        Avg_B2 = B;

                        Avg_R1 = Avg_R1 + (Avg_R1 >> 5);
                        Avg_G1 = Avg_G1 + (Avg_G1 >> 5);
                        Avg_B1 = Avg_B1 + (Avg_B1 >> 5);

                        Avg_R2 = (Avg_R2 << 3) + (Avg_R2 >> 2);
                        Avg_G2 = (Avg_G2 << 3) + (Avg_G2 >> 2);
                        Avg_B2 = (Avg_B2 << 3) + (Avg_B2 >> 2);
                    }
                    else
                    {
                        // +============+
                        // | Individual |
                        // +============+
                        if ((Avg_R1 & 0xF) > 7)
                            Avg_R1 = Clip(Avg_R1 + 0x10);
                        if ((Avg_G1 & 0xF) > 7)
                            Avg_G1 = Clip(Avg_G1 + 0x10);
                        if ((Avg_B1 & 0xF) > 7)
                            Avg_B1 = Clip(Avg_B1 + 0x10);
                        if ((Avg_R2 & 0xF) > 7)
                            Avg_R2 = Clip(Avg_R2 + 0x10);
                        if ((Avg_G2 & 0xF) > 7)
                            Avg_G2 = Clip(Avg_G2 + 0x10);
                        if ((Avg_B2 & 0xF) > 7)
                            Avg_B2 = Clip(Avg_B2 + 0x10);

                        Block_Top = ((Avg_R2 & 0xF0) >> 4) | (Avg_R1 & 0xF0);
                        Block_Top = Block_Top | (((Avg_G2 & 0xF0) << 4) | ((Avg_G1 & 0xF0) << 8));
                        Block_Top = Block_Top | (((Avg_B2 & 0xF0) << 12) | ((Avg_B1 & 0xF0) << 16));

                        // Vamos ter certeza de que os mesmos valores obtidos pelo descompressor serão usados na comparação (modo Individual)
                        Avg_R1 = (Avg_R1 & 0xF0) + ((Avg_R1 & 0xF0) >> 4);
                        Avg_G1 = (Avg_G1 & 0xF0) + ((Avg_G1 & 0xF0) >> 4);
                        Avg_B1 = (Avg_B1 & 0xF0) + ((Avg_B1 & 0xF0) >> 4);

                        Avg_R2 = (Avg_R2 & 0xF0) + ((Avg_R2 & 0xF0) >> 4);
                        Avg_G2 = (Avg_G2 & 0xF0) + ((Avg_G2 & 0xF0) >> 4);
                        Avg_B2 = (Avg_B2 & 0xF0) + ((Avg_B2 & 0xF0) >> 4);
                    }

                    if (Flip)
                        Block_Top = Block_Top | 0x1000000;
                    if (Difference)
                        Block_Top = Block_Top | 0x2000000;

                    // Seleciona a melhor tabela para ser usada nos blocos
                    int Mod_Table_1 = 0;
                    int[] Min_Diff_1 = new int[8];
                    for (int a = 0; a <= 7; a++)
                        Min_Diff_1[a] = 0;
                    for (int Y = 0; Y <= (Flip ? 1 : 3); Y++)
                    {
                        for (int X = 0; X <= (Flip ? 3 : 1); X++)
                        {
                            int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Luma = Convert.ToInt32(0.299F * Out[Image_Offset] + 0.587F * Out[Image_Offset + 1] + 0.114F * Out[Image_Offset + 2]);

                            for (int a = 0; a <= 7; a++)
                            {
                                int Optimal_Diff = 255 * 4;
                                for (int b = 0; b <= 3; b++)
                                {
                                    int CR = Clip(Avg_R1 + Modulation_Table[a, b]);
                                    int CG = Clip(Avg_G1 + Modulation_Table[a, b]);
                                    int CB = Clip(Avg_B1 + Modulation_Table[a, b]);

                                    int Test_Luma = Convert.ToInt32(0.299F * CR + 0.587F * CG + 0.114F * CB);
                                    int Diff = Math.Abs(Luma - Test_Luma);
                                    if (Diff < Optimal_Diff)
                                        Optimal_Diff = Diff;
                                }
                                Min_Diff_1[a] += Optimal_Diff;
                            }
                        }
                    }

                    int Temp_1 = 255 * 8;
                    for (int a = 0; a <= 7; a++)
                    {
                        if (Min_Diff_1[a] < Temp_1)
                        {
                            Temp_1 = Min_Diff_1[a];
                            Mod_Table_1 = a;
                        }
                    }

                    int Mod_Table_2 = 0;
                    int[] Min_Diff_2 = new int[8];
                    for (int a = 0; a <= 7; a++)
                        Min_Diff_2[a] = 0;
                    for (int Y = Flip ? 2 : 0; Y <= 3; Y++)
                    {
                        for (int X = Flip ? 0 : 2; X <= 3; X++)
                        {
                            int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Luma = Convert.ToInt32(0.299F * Out[Image_Offset] + 0.587F * Out[Image_Offset + 1] + 0.114F * Out[Image_Offset + 2]);

                            for (int a = 0; a <= 7; a++)
                            {
                                int Optimal_Diff = 255 * 4;
                                for (int b = 0; b <= 3; b++)
                                {
                                    int CR = Clip(Avg_R2 + Modulation_Table[a, b]);
                                    int CG = Clip(Avg_G2 + Modulation_Table[a, b]);
                                    int CB = Clip(Avg_B2 + Modulation_Table[a, b]);

                                    int Test_Luma = Convert.ToInt32(0.299F * CR + 0.587F * CG + 0.114F * CB);
                                    int Diff = Math.Abs(Luma - Test_Luma);
                                    if (Diff < Optimal_Diff)
                                        Optimal_Diff = Diff;
                                }
                                Min_Diff_2[a] += Optimal_Diff;
                            }
                        }
                    }

                    int Temp_2 = 255 * 8;
                    for (int a = 0; a <= 7; a++)
                    {
                        if (Min_Diff_2[a] < Temp_2)
                        {
                            Temp_2 = Min_Diff_2[a];
                            Mod_Table_2 = a;
                        }
                    }

                    Block_Top = Block_Top | (Mod_Table_1 << 29);
                    Block_Top = Block_Top | (Mod_Table_2 << 26);

                    // Seleciona o melhor valor da tabela que mais se aproxima com a cor original
                    for (int Y = 0; Y <= (Flip ? 1 : 3); Y++)
                    {
                        for (int X = 0; X <= (Flip ? 3 : 1); X++)
                        {
                            int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Luma = Convert.ToInt32(0.299F * Out[Image_Offset] + 0.587F * Out[Image_Offset + 1] + 0.114F * Out[Image_Offset + 2]);

                            int Col_Diff = 255;
                            int Pix_Table_Index = 0;
                            for (int b = 0; b <= 3; b++)
                            {
                                int CR = Clip(Avg_R1 + Modulation_Table[Mod_Table_1, b]);
                                int CG = Clip(Avg_G1 + Modulation_Table[Mod_Table_1, b]);
                                int CB = Clip(Avg_B1 + Modulation_Table[Mod_Table_1, b]);

                                int Test_Luma = Convert.ToInt32(0.299F * CR + 0.587F * CG + 0.114F * CB);
                                int Diff = Math.Abs(Luma - Test_Luma);
                                if (Diff < Col_Diff)
                                {
                                    Col_Diff = Diff;
                                    Pix_Table_Index = b;
                                }
                            }

                            int Index = X * 4 + Y;
                            if (Index < 8)
                            {
                                Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index + 8));
                                Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 24));
                            }
                            else
                            {
                                Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index - 8));
                                Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 8));
                            }
                        }
                    }

                    for (int Y = Flip ? 2 : 0; Y <= 3; Y++)
                    {
                        for (int X = Flip ? 0 : 2; X <= 3; X++)
                        {
                            int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Luma = Convert.ToInt32(0.299F * Out[Image_Offset] + 0.587F * Out[Image_Offset + 1] + 0.114F * Out[Image_Offset + 2]);

                            int Col_Diff = 255;
                            int Pix_Table_Index = 0;
                            for (int b = 0; b <= 3; b++)
                            {
                                int CR = Clip(Avg_R2 + Modulation_Table[Mod_Table_2, b]);
                                int CG = Clip(Avg_G2 + Modulation_Table[Mod_Table_2, b]);
                                int CB = Clip(Avg_B2 + Modulation_Table[Mod_Table_2, b]);

                                int Test_Luma = Convert.ToInt32(0.299F * CR + 0.587F * CG + 0.114F * CB);
                                int Diff = Math.Abs(Luma - Test_Luma);
                                if (Diff < Col_Diff)
                                {
                                    Col_Diff = Diff;
                                    Pix_Table_Index = b;
                                }
                            }

                            int Index = X * 4 + Y;
                            if (Index < 8)
                            {
                                Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index + 8));
                                Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 24));
                            }
                            else
                            {
                                Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index - 8));
                                Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 8));
                            }
                        }
                    }

                    // Copia dados para a saída
                    byte[] Block = new byte[8];
                    Buffer.BlockCopy(BitConverter.GetBytes(Block_Top), 0, Block, 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(Block_Bottom), 0, Block, 4, 4);
                    byte[] New_Block = new byte[8];
                    for (int j = 0; j <= 7; j++)
                        New_Block[7 - j] = Block[j];
                    if (format == PICATextureFormat.ETC1A4)
                    {
                        byte[] Alphas = new byte[8];
                        int Alpha_Offset = 0;
                        for (int TX = 0; TX <= 3; TX++)
                        {
                            for (int TY = 0; TY <= 3; TY += 2)
                            {
                                int Img_Offset_1 = (Tile_X * 4 + TX + ((Tile_Y * 4 + TY) * Width)) * 4;
                                int Img_Offset_2 = (Tile_X * 4 + TX + ((Tile_Y * 4 + TY + 1) * Width)) * 4;

                                byte Alpha_1 = (byte)(Out[Img_Offset_1 + 3] >> 4);
                                byte Alpha_2 = (byte)(Out[Img_Offset_2 + 3] >> 4);

                                Alphas[Alpha_Offset] = (byte)(Alpha_1 | (Alpha_2 << 4));

                                Alpha_Offset += 1;
                            }
                        }

                        Buffer.BlockCopy(Alphas, 0, Out_Data, Out_Data_Offset, 8);
                        Buffer.BlockCopy(New_Block, 0, Out_Data, Out_Data_Offset + 8, 8);
                        Out_Data_Offset += 16;
                    }
                    else if (format == PICATextureFormat.ETC1)
                    {
                        Buffer.BlockCopy(New_Block, 0, Out_Data, Out_Data_Offset, 8);
                        Out_Data_Offset += 8;
                    }
                }
            }

            return Out_Data;
        }

        private static int[] Get_ETC1_Scramble(int Width, int Height)
        {
            int[] Tile_Scramble = new int[((Width / 4) * (Height / 4)) - 1 + 1];
            int Base_Accumulator = 0;
            int Line_Accumulator = 0;
            int Base_Number = 0;
            int Line_Number = 0;

            for (int Tile = 0; Tile <= Tile_Scramble.Length - 1; Tile++)
            {
                if ((Tile % (Width / 4) == 0) & Tile > 0)
                {
                    if (Line_Accumulator < 1)
                    {
                        Line_Accumulator += 1;
                        Line_Number += 2;
                        Base_Number = Line_Number;
                    }
                    else
                    {
                        Line_Accumulator = 0;
                        Base_Number -= 2;
                        Line_Number = Base_Number;
                    }
                }

                Tile_Scramble[Tile] = Base_Number;

                if (Base_Accumulator < 1)
                {
                    Base_Accumulator += 1;
                    Base_Number += 1;
                }
                else
                {
                    Base_Accumulator = 0;
                    Base_Number += 3;
                }
            }

            return Tile_Scramble;
        }

        public static int CalculateLength(int Width, int Height, PICATextureFormat Format)
        {
            int Length = (Width * Height * FmtBPP[(int)Format]) / 8;

            if ((Length & 0x7f) != 0)
            {
                Length = (Length & ~0x7f) + 0x80;
            }

            return Length;
        }

        private static sbyte Signed_Byte(byte Byte_To_Convert)
        {
            if ((Byte_To_Convert < 0x80))
                return Convert.ToSByte(Byte_To_Convert);
            return Convert.ToSByte(Byte_To_Convert - 0x100);
        }

        private static byte Clip(int Value)
        {
            if (Value > 0xFF)
                return 0xFF;
            else if (Value < 0)
                return 0;
            else
                return Convert.ToByte(Value & 0xFF);
        }

        public static Bitmap GetBitmap(byte[] Buffer, int Width, int Height)
        {
            Rectangle Rect = new Rectangle(0, 0, Width, Height);

            Bitmap Img = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);

            BitmapData ImgData = Img.LockBits(Rect, ImageLockMode.WriteOnly, Img.PixelFormat);

            Marshal.Copy(Buffer, 0, ImgData.Scan0, Buffer.Length);

            Img.UnlockBits(ImgData);

            return Img;
        }

        public static byte[] GetBuffer(Bitmap Img)
        {
            Rectangle Rect = new Rectangle(0, 0, Img.Width, Img.Height);

            BitmapData ImgData = Img.LockBits(Rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte[] Output = new byte[ImgData.Stride * Img.Height];

            Marshal.Copy(ImgData.Scan0, Output, 0, Output.Length);

            Img.UnlockBits(ImgData);

            return Output;
        }
    }
}
