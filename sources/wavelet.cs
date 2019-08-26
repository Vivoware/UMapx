﻿// UMapx.NET framework
// Digital Signal Processing Library.
// 
// Copyright © UMapx.NET, 2015-2019
// Asiryan Valeriy
// Moscow, Russia
// Version 4.0.0

using System;
using System.Runtime.Serialization;
using UMapx.Core;
using UMapx.Transform;

namespace UMapx.Wavelet
{
    // **************************************************************************
    //                              WAVELET TOOLBOX
    //                            UMAPX.NET FRAMEWORK
    // **************************************************************************
    // Wavelet Toolbox provides a wide functionality for the study discrete 
    // and continuous wavelets. It includes algorithms for discrete one- and two-
    // dimensional wavelet transforms of real and complex signals.
    // **************************************************************************
    // Designed by Asiryan Valeriy (c), 2015-2019
    // Moscow, Russia.
    // **************************************************************************

    #region Wavelet Transforms
    /// <summary>
    /// Определяет дискретное вейвлет-преобразование.
    /// <remarks>
    /// Для корректного вейвлет-преобразования исходного сигнала необходимо, чтобы его размерность была степенью 2.
    /// 
    /// Более подробную информацию можно найти на сайте:
    /// https://en.wikipedia.org/wiki/Discrete_wavelet_transform
    /// </remarks>
    /// </summary>
    public class WaveletTransform : IWaveletTransform, ILevelTransform, ITransform
    {
        #region Private data
        private double[] lp;        // Low-Pass filter,
        private double[] hp;        // High-Pass filer,
        private double[] ilp;       // Inverse Low-Pass filter,
        private double[] ihp;       // Inverse High-Pass filter,
        private bool normalized;    // Normalized transform or not,
        private int levels;         // Number of levels.
        #endregion

        #region Initialize
        /// <summary>
        /// Инициализирует дискретное вейвлет-преобразование.
        /// </summary>
        /// <param name="wavelet">Дискретный вейвлет</param>
        /// <param name="levels">Количество уровней</param>
        /// <param name="normalized">Нормализованное преобразование или нет</param>
        public WaveletTransform(WaveletPack wavelet, int levels = 1, bool normalized = true)
        {
            Wavelet = wavelet; Levels = levels; Normalized = normalized;
        }
        /// <summary>
        /// Получает или задает количество уровней преобразования.
        /// </summary>
        public int Levels
        {
            get
            {
                return this.levels;
            }
            set
            {
                if (value < 1)
                    throw new Exception("Количество уровней не может быть меньше 1");

                this.levels = value;
            }
        }
        /// <summary>
        /// Нормализированное преобразование или нет.
        /// </summary>
        public bool Normalized
        {
            get
            {
                return this.normalized;
            }
            set
            {
                this.normalized = value;
            }
        }
        /// <summary>
        /// Получает или задает дискретный вейвлет.
        /// </summary>
        public WaveletPack Wavelet
        {
            get
            {
                return new WaveletPack(lp, hp, ilp, ihp);
            }
            set
            {
                this.lp  = value.LowPass;
                this.hp  = value.HighPass;
                this.ilp = value.iLowPass;
                this.ihp = value.iHighPass;
            }
        }
        #endregion

        #region Wavelet transform
        /// <summary>
        /// Прямое вейвлет-преобразование.
        /// </summary>
        /// <param name="A">Одномерный массив</param>
        /// <returns>Одномерный массив</returns>
        public double[] Forward(double[] A)
        {
            // params
            int nLevels = (int)Math.Min(Maths.Log2(A.Length),this.levels);

            // forward multi-scale wavelet transform
            for (int i = 0; i < this.levels; i++)
            {
                A = this.dwt(A, i);
            }

            return A;
        }
        /// <summary>
        /// Обратное вейвлет-преобразование.
        /// </summary>
        /// <param name="B">Одномерный массив</param>
        /// <returns>Одномерный массив</returns>
        public double[] Backward(double[] B)
        {
            // params
            int nLevels = (int)Math.Min(Maths.Log2(B.Length), this.levels);

            // backward multi-scale wavelet transform
            for (int i = this.levels; i > 0; i--)
            {
                B = this.idwt(B, i);
            }

            return B;
        }
        /// <summary>
        /// Прямое вейвлет-преобразование.
        /// </summary>
        /// <param name="A">Двумерный массив</param>
        /// <returns>Двумерный массив</returns>
        public double[,] Forward(double[,] A)
        {
            // params
            int Bound1, Bound2, i, j;
            int DataLen1 = A.GetLength(0);
            int DataLen2 = A.GetLength(1);
            double[,] output = (double[,])A.Clone();
            double[] buff2 = new double[DataLen2];
            double[] buff1 = new double[DataLen1];
            int nLevels = (int)Math.Min(Math.Min(Maths.Log2(DataLen1), 
                this.levels), DataLen2);

            // do job
            for (int lev = 0; lev < nLevels; lev++)
            {
                Bound1 = DataLen1 >> lev;
                Bound2 = DataLen2 >> lev;

                if (!Maths.IsEven(Bound1) && Bound1 < DataLen1)
                    Bound1--;
                if (!Maths.IsEven(Bound2) && Bound2 < DataLen2)
                    Bound2--;

                for (i = 0; i < Bound1; i++)
                {
                    for (j = 0; j < Bound2; j++) buff2[j] = output[i, j];
                    buff2 = this.dwt(buff2, lev);
                    for (j = 0; j < Bound2; j++) output[i, j] = buff2[j];
                }

                for (j = 0; j < Bound2; j++)
                {
                    for (i = 0; i < Bound1; i++) buff1[i] = output[i, j];
                    buff1 = this.dwt(buff1, lev);
                    for (i = 0; i < Bound1; i++) output[i, j] = buff1[i];
                }
            }

            return output;
        }
        /// <summary>
        /// Обратное вейвлет-преобразование.
        /// </summary>
        /// <param name="B">Двумерный массив</param>
        /// <returns>Двумерный массив</returns>
        public double[,] Backward(double[,] B)
        {
            // params
            int Bound1, Bound2, i, j;
            int DataLen1 = B.GetLength(0);
            int DataLen2 = B.GetLength(1);
            double[,] output = (double[,])B.Clone();
            double[] buff1 = new double[DataLen1];
            double[] buff2 = new double[DataLen2];
            int nLevels = (int)Math.Min(Math.Min(Maths.Log2(DataLen1),
    this.levels), DataLen2);

            // do job
            for (int lev = nLevels; lev > 0; lev--)
            {
                Bound1 = DataLen1 >> lev;
                Bound2 = DataLen2 >> lev;

                for (i = 0; i < Bound1 << 1; i++)
                {
                    for (j = 0; j < Bound2 << 1; j++) buff2[j] = output[i, j];
                    buff2 = this.idwt(buff2, lev);
                    for (j = 0; j < Bound2 << 1; j++) output[i, j] = buff2[j];
                }

                for (j = 0; j < Bound2 << 1; j++)
                {
                    for (i = 0; i < Bound1 << 1; i++) buff1[i] = output[i, j];
                    buff1 = this.idwt(buff1, lev);
                    for (i = 0; i < Bound1 << 1; i++) output[i, j] = buff1[i];
                }
            }

            return output;
        }
        /// <summary>
        /// Прямое вейвлет-преобразование.
        /// </summary>
        /// <param name="A">Одномерный массив</param>
        /// <returns>Одномерный массив</returns>
        public Complex[] Forward(Complex[] A)
        {
            // params
            int nLevels = (int)Math.Min(Maths.Log2(A.Length), this.levels);

            // forward multi-scale wavelet transform
            for (int i = 0; i < this.levels; i++)
            {
                A = this.dwt(A, i);
            }

            return A;
        }
        /// <summary>
        /// Обратное вейвлет-преобразование.
        /// </summary>
        /// <param name="B">Одномерный массив</param>
        /// <returns>Одномерный массив</returns>
        public Complex[] Backward(Complex[] B)
        {
            // params
            int nLevels = (int)Math.Min(Maths.Log2(B.Length), this.levels);

            // backward multi-scale wavelet transform
            for (int i = this.levels; i > 0; i--)
            {
                B = this.idwt(B, i);
            }

            return B;
        }
        /// <summary>
        /// Прямое вейвлет-преобразование.
        /// </summary>
        /// <param name="A">Двумерный массив</param>
        /// <returns>Двумерный массив</returns>
        public Complex[,] Forward(Complex[,] A)
        {
            // params
            int Bound1, Bound2, i, j;
            int DataLen1 = A.GetLength(0);
            int DataLen2 = A.GetLength(1);
            Complex[,] output = (Complex[,])A.Clone();
            Complex[] buff2 = new Complex[DataLen2];
            Complex[] buff1 = new Complex[DataLen1];
            int nLevels = (int)Math.Min(Math.Min(Maths.Log2(DataLen1),
this.levels), DataLen2);

            // do job
            for (int lev = 0; lev < nLevels; lev++)
            {
                Bound1 = DataLen1 >> lev;
                Bound2 = DataLen2 >> lev;

                if (!Maths.IsEven(Bound1) && Bound1 < DataLen1)
                    Bound1--;
                if (!Maths.IsEven(Bound2) && Bound2 < DataLen2)
                    Bound2--;

                for (i = 0; i < Bound1; i++)
                {
                    for (j = 0; j < Bound2; j++) buff2[j] = output[i, j];
                    buff2 = this.dwt(buff2, lev);
                    for (j = 0; j < Bound2; j++) output[i, j] = buff2[j];
                }

                for (j = 0; j < Bound2; j++)
                {
                    for (i = 0; i < Bound1; i++) buff1[i] = output[i, j];
                    buff1 = this.dwt(buff1, lev);
                    for (i = 0; i < Bound1; i++) output[i, j] = buff1[i];
                }
            }

            return output;
        }
        /// <summary>
        /// Обратное вейвлет-преобразование.
        /// </summary>
        /// <param name="B">Двумерный массив</param>
        /// <returns>Двумерный массив</returns>
        public Complex[,] Backward(Complex[,] B)
        {
            // params
            int Bound1, Bound2, i, j;
            int DataLen1 = B.GetLength(0);
            int DataLen2 = B.GetLength(1);
            Complex[,] output = (Complex[,])B.Clone();
            Complex[] buff1 = new Complex[DataLen1];
            Complex[] buff2 = new Complex[DataLen2];
            int nLevels = (int)Math.Min(Math.Min(Maths.Log2(DataLen1),
    this.levels), DataLen2);

            // do job
            for (int lev = nLevels; lev > 0; lev--)
            {
                Bound1 = DataLen1 >> lev;
                Bound2 = DataLen2 >> lev;

                for (i = 0; i < Bound1 << 1; i++)
                {
                    for (j = 0; j < Bound2 << 1; j++) buff2[j] = output[i, j];
                    buff2 = this.idwt(buff2, lev);
                    for (j = 0; j < Bound2 << 1; j++) output[i, j] = buff2[j];
                }

                for (j = 0; j < Bound2 << 1; j++)
                {
                    for (i = 0; i < Bound1 << 1; i++) buff1[i] = output[i, j];
                    buff1 = this.idwt(buff1, lev);
                    for (i = 0; i < Bound1 << 1; i++) output[i, j] = buff1[i];
                }
            }

            return output;
        }
        #endregion

        #region Private voids
        /// <summary>
        /// Forward discrete wavelet transform.
        /// </summary>
        /// <param name="input">Input signal</param>
        /// <param name="level">Current level of transform</param>
        /// <returns>Output data</returns>
        private double[] dwt(double[] input, int level)
        {
            // params
            int length = input.Length;
            double[] output = new double[length];
            int Bound = length >> level;

            // odd element
            if (!Maths.IsEven(Bound))
            {
                Bound--;
            }

            int lpLen = this.lp.Length;
            int hpLen = this.hp.Length;
            int lpStart = -((lpLen >> 1) - 1);
            int hpStart = -((hpLen >> 1) - 1);
            Array.Copy(input, Bound, output, Bound, length - Bound);
            double a = 0;
            double b = 0;
            int h = Bound >> 1;
            int c, i, j, r, k;

            // do job
            for (i = 0, r = 0; i < Bound; i += 2, r++)
            {
                // low-pass filter
                for (j = lpStart, k = 0; k < lpLen; j++, k++)
                {
                    if (j < 0 || j >= Bound)
                        c = (j % Bound + Bound) % Bound;
                    else
                        c = j;
                    a += this.lp[k] * input[c];
                }

                // high-pass filter
                for (j = hpStart, k = 0; k < hpLen; j++, k++)
                {
                    if (j < 0 || j >= Bound)
                        c = (j % Bound + Bound) % Bound;
                    else
                        c = j;
                    b += this.hp[k] * input[c];
                }
                lpStart += 2;
                hpStart += 2;

                if (normalized)
                {
                    output[r    ] = a / Maths.Sqrt2;
                    output[r + h] = b / Maths.Sqrt2;
                }
                else
                {
                    output[r    ] = a;
                    output[r + h] = b;
                }

                a = 0;
                b = 0;
            }

            return output;
        }
        /// <summary>
        /// Backward discrete wavelet transform.
        /// </summary>
        /// <param name="input">Input signal</param>
        /// <param name="level">Current level of transform</param>
        /// <returns>Output data</returns>
        private double[] idwt(double[] input, int level)
        {
            // params
            int length = input.Length;
            double[] output = (double[])input.Clone();
            int Bound = length >> level;
            int h = Bound << 1;
            int lpLen = this.ilp.Length;
            int hpLen = this.ihp.Length;
            int lpStart = -((lpLen >> 1) - 1);
            int hpStart = -((hpLen >> 1) - 1);
            double[] Low = new double[h];
            double[] Hig = new double[h];
            double s = 0;
            int c, i, j, k;

            // redim
            for (i = 0, j = 0; i < h; i += 2, j++)
            {
                Low[i    ] = 0;
                Hig[i    ] = 0;
                Low[i + 1] = input[j];
                Hig[i + 1] = input[Bound + j];
            }

            // do job
            for (i = 0; i < h; i++)
            {
                // low-pass filter
                for (j = lpStart, k = 0; k < lpLen; j++, k++)
                {
                    if (j < 0 || j >= h)
                        c = (j % h + h) % h;
                    else
                        c = j;
                    s += this.ilp[k] * Low[c];
                }

                // high-pass filter
                for (j = hpStart, k = 0; k < hpLen; j++, k++)
                {
                    if (j < 0 || j >= h)
                        c = (j % h + h) % h;
                    else
                        c = j;
                    s += this.ihp[k] * Hig[c];
                }

                lpStart += 1;
                hpStart += 1;
                output[i] = (normalized) ? s * Maths.Sqrt2 : s;
                s = 0;
            }

            return output;
        }

        /// <summary>
        /// Forward discrete wavelet transform.
        /// </summary>
        /// <param name="input">Input signal</param>
        /// <param name="level">Current level of transform</param>
        /// <returns>Output data</returns>
        private Complex[] dwt(Complex[] input, int level)
        {
            // params
            int length = input.Length;
            Complex[] output = new Complex[length];
            int Bound = length >> level;

            // odd element
            if (!Maths.IsEven(Bound))
            {
                Bound--;
            }

            int lpLen = this.lp.Length;
            int hpLen = this.hp.Length;
            int lpStart = -((lpLen >> 1) - 1);
            int hpStart = -((hpLen >> 1) - 1);
            Array.Copy(input, Bound, output, Bound, length - Bound);
            Complex a = 0;
            Complex b = 0;
            int h = Bound >> 1;
            int c, i, j, r, k;

            // do job
            for (i = 0, r = 0; i < Bound; i += 2, r++)
            {
                // low-pass filter
                for (j = lpStart, k = 0; k < lpLen; j++, k++)
                {
                    if (j < 0 || j >= Bound)
                        c = (j % Bound + Bound) % Bound;
                    else
                        c = j;
                    a += this.lp[k] * input[c];
                }

                // high-pass filter
                for (j = hpStart, k = 0; k < hpLen; j++, k++)
                {
                    if (j < 0 || j >= Bound)
                        c = (j % Bound + Bound) % Bound;
                    else
                        c = j;
                    b += this.hp[k] * input[c];
                }
                lpStart += 2;
                hpStart += 2;

                if (normalized)
                {
                    output[r] = a / Maths.Sqrt2;
                    output[r + h] = b / Maths.Sqrt2;
                }
                else
                {
                    output[r] = a;
                    output[r + h] = b;
                }

                a = 0;
                b = 0;
            }

            return output;
        }
        /// <summary>
        /// Backward discrete wavelet transform.
        /// </summary>
        /// <param name="input">Input signal</param>
        /// <param name="level">Current level of transform</param>
        /// <returns>Output data</returns>
        private Complex[] idwt(Complex[] input, int level)
        {
            // params
            int length = input.Length;
            Complex[] output = (Complex[])input.Clone();
            int Bound = length >> level;
            int h = Bound << 1;
            int lpLen = this.ilp.Length;
            int hpLen = this.ihp.Length;
            int lpStart = -((lpLen >> 1) - 1);
            int hpStart = -((hpLen >> 1) - 1);
            Complex[] Low = new Complex[h];
            Complex[] Hig = new Complex[h];
            Complex s = 0;
            int c, i, j, k;

            // redim
            for (i = 0, j = 0; i < h; i += 2, j++)
            {
                Low[i] = 0;
                Hig[i] = 0;
                Low[i + 1] = input[j];
                Hig[i + 1] = input[Bound + j];
            }

            // do job
            for (i = 0; i < h; i++)
            {
                // low-pass filter
                for (j = lpStart, k = 0; k < lpLen; j++, k++)
                {
                    if (j < 0 || j >= h)
                        c = (j % h + h) % h;
                    else
                        c = j;
                    s += this.ilp[k] * Low[c];
                }

                // high-pass filter
                for (j = hpStart, k = 0; k < hpLen; j++, k++)
                {
                    if (j < 0 || j >= h)
                        c = (j % h + h) % h;
                    else
                        c = j;
                    s += this.ihp[k] * Hig[c];
                }

                lpStart += 1;
                hpStart += 1;
                output[i] = (normalized) ? s * Maths.Sqrt2 : s;
                s = 0;
            }

            return output;
        }
        #endregion
    }
    /// <summary>
    /// Определяет дискретный вейвлет.
    /// <remarks>
    /// Более подробную информацию можно найти на сайте:
    /// https://en.wikipedia.org/wiki/Wavelet
    /// </remarks>
    /// </summary>
    public struct WaveletPack : ICloneable, ISerializable
    {
        #region Private data
        private double[] lp;        // Low-Pass filter
        private double[] hp;        // High-Pass filer
        private double[] ilp;       // Inverse Low-Pass filter
        private double[] ihp;       // Inverse High-Pass filter
        #endregion

        #region Wavelet components
        /// <summary>
        /// Инициализирует дискретный вейвлет.
        /// </summary>
        /// <param name="lp">Масштабирующая функция прямого преобразования</param>
        /// <param name="hp">Вейвлет-функция прямого преобразования</param>
        /// <param name="ilp">Масштабирующая функция обратного преобразования</param>
        /// <param name="ihp">Вейвлет-функция обратного преобразования</param>
        public WaveletPack(double[] lp, double[] hp, double[] ilp, double[] ihp)
        {
            this.lp = lp; this.hp = hp; this.ilp = ilp; this.ihp = ihp;
        }
        /// <summary>
        /// Получает или задает масштабирующую функцию прямого преобразования.
        /// </summary>
        public double[] LowPass
        {
            get
            {
                return this.lp;
            }
            set
            {
                this.lp = value;
            }
        }
        /// <summary>
        /// Получает или задает вейвлет-функцию прямого преобразования.
        /// </summary>
        public double[] HighPass
        {
            get
            {
                return this.hp;
            }
            set
            {
                this.hp = value;
            }
        }
        /// <summary>
        /// Получает или задает масштабирующую функцию обратного преобразования.
        /// </summary>
        public double[] iLowPass
        {
            get
            {
                return ilp;
            }
            set
            {
                this.ilp = value;
            }
        }
        /// <summary>
        /// Получает или задает вейвлет-функцию обратного преобразования.
        /// </summary>
        public double[] iHighPass
        {
            get
            {
                return ihp;
            }
            set
            {
                this.ihp = value;
            }
        }
        #endregion

        #region Public static voids
        /// <summary>
        /// Инвертирует нечетные элементы вектора.
        /// </summary>
        /// <param name="v">Одномерный массив</param>
        /// <returns>Одномерный массив</returns>
        public static double[] InvertOdds(double[] v)
        {
            double[] w = (double[])v.Clone();
            int length = w.Length, i;

            // inversion of odd elements:
            for (i = 1; i < length; i += 2)
            {
                w[i] = -w[i];
            }
            return w;
        }
        /// <summary>
        /// Инвертирует четные элементы вектора.
        /// </summary>
        /// <param name="v">Одномерный массив</param>
        /// <returns>Одномерный массив</returns>
        public static double[] InvertEvens(double[] v)
        {
            double[] w = (double[])v.Clone();
            int length = w.Length, i;

            // inversion of even elements:
            for (i = 0; i < length; i += 2)
            {
                w[i] = -w[i];
            }
            return w;
        }
        /// <summary>
        /// Возвращает вейвлет-функцию Добеши.
        /// </summary>
        /// <param name="scaling">Масштабирующая функция</param>
        /// <returns>Вейвлет-функция</returns>
        private static double[] GetWavelet(double[] scaling)
        {
            return WaveletPack.InvertOdds(Matrice.Flip(scaling));
        }
        /// <summary>
        /// Создает дискретный вейвлет.
        /// </summary>
        /// <param name="scaling">Масштабирующая функция</param>
        /// <returns>Вейвлет-фильтр</returns>
        public static WaveletPack Create(double[] scaling)
        {
            double[] lp = scaling;
            double[] hp = WaveletPack.GetWavelet(lp);
            double[] ilp = Matrice.Flip(lp);
            double[] ihp = Matrice.Flip(hp);

            return new WaveletPack(lp, hp, ilp, ihp);
        }
        /// <summary>
        /// Создает дискретный вейвлет.
        /// </summary>
        /// <param name="scaling">Масштабирующая функция</param>
        /// <param name="wavelet">Вейвлет-функция</param>
        /// <returns>Вейвлет-фильтр</returns>
        public static WaveletPack Create(double[] scaling, double[] wavelet)
        {
            double[] lp = scaling;
            double[] hp = wavelet;
            double[] ilp = WaveletPack.InvertEvens(hp);
            double[] ihp = WaveletPack.InvertOdds(lp);

            return new WaveletPack(lp, hp, ilp, ihp);
        }
        #endregion

        #region Clone members
        /// <summary>
        /// Создает копию дискретного вейвлета.
        /// </summary>
        /// <returns>Комплексное число</returns>
        object ICloneable.Clone()
        {
            return new WaveletPack(
                (double[])this.lp.Clone(),
                (double[])this.hp.Clone(), 
                (double[])this.ilp.Clone(), 
                (double[])this.ihp.Clone());
        }
        /// <summary>
        /// Создает копию дискретного вейвлета.
        /// </summary>
        /// <returns>Комплексное число</returns>
        public WaveletPack Clone()
        {
            return new WaveletPack(
                (double[])this.lp.Clone(),
                (double[])this.hp.Clone(),
                (double[])this.ilp.Clone(),
                (double[])this.ihp.Clone());
        }
        #endregion

        #region Serialization members
        /// <summary>
        /// Получает информацию об объекте.
        /// </summary>
        /// <param name="info">Данные, необходимые для сериализации и диссериализации объекта</param>
        /// <param name="context">Источник и назначение заданного потока</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Масштабирующая функция прямого преобразования", this.lp);
            info.AddValue("Вейвлет-функция прямого преобразования", this.hp);
            info.AddValue("Масштабирующая функция обратного преобразования", this.ilp);
            info.AddValue("Вейвлет-функция обратного преобразования", this.ihp);
        }
        #endregion

        #region Biorthogonal wavelets
        /// <summary>
        /// Возвращает биортогональный вейвлет 1.1.
        /// <remarks>
        /// Вейвлет Хаара.
        /// </remarks>
        /// </summary>
        public static WaveletPack Bior11
        {
            get
            {
                // Haar's wavelet:
                return Create(new double[] { 0.707106781186548, 0.707106781186548 }, new double[] { -0.707106781186548, 0.707106781186548 });
            }
        }
        /// <summary>
        /// Возвращает биортогональный вейвлет 1.3.
        /// </summary>
        public static WaveletPack Bior13
        {
            get
            {
                return Create(new double[] { -0.088388347648318, 0.088388347648318, 0.707106781186548, 0.707106781186548, 0.088388347648318, -0.088388347648318 }, new double[] { 0, 0, -0.707106781186548, 0.707106781186548, 0, 0 });
            }
        }
        /// <summary>
        /// Возвращает биортогональный вейвлет 1.5.
        /// </summary>
        public static WaveletPack Bior15
        {
            get
            {
                return Create(new double[] { 0.016572815184060, -0.016572815184060, -0.121533978016438, 0.121533978016438, 0.707106781186548, 0.707106781186548, 0.121533978016438, -0.121533978016438, -0.016572815184060, 0.016572815184060 }, new double[] { 0, 0, 0, 0, -0.707106781186548, 0.707106781186548, 0, 0, 0, 0 });
            }
        }
        /// <summary>
        /// Возвращает биортогональный вейвлет 2.2.
        /// </summary>
        public static WaveletPack Bior22
        {
            get
            {
                return Create(new double[] { 0, -0.176776695296637, 0.353553390593274, 1.060660171779821, 0.353553390593274, -0.176776695296637 }, new double[] { 0, 0.353553390593274, -0.707106781186548, 0.353553390593274, 0, 0 });
            }
        }
        /// <summary>
        /// Возвращает биортогональный вейвлет 2.4.
        /// </summary>
        public static WaveletPack Bior24
        {
            get
            {
                return Create(new double[] { 
                   0.000000000000000,
                   0.033145630368119,
                  -0.066291260736239,
                  -0.176776695296637,
                   0.419844651329513,
                   0.994368911043582,
                   0.419844651329513,
                  -0.176776695296637,
                  -0.066291260736239,
                   0.033145630368119 }, new double[] { 
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.353553390593274, 
                  -0.707106781186548, 
                   0.353553390593274,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000 });
            }
        }
        /// <summary>
        /// Возвращает биортогональный вейвлет 2.6.
        /// </summary>
        public static WaveletPack Bior26
        {
            get
            {
                return Create(new double[] { 
                   0.000000000000000,
                  -0.006905339660025,
                   0.013810679320050,
                   0.046956309688169,
                  -0.107723298696388,
                  -0.169871355636612,
                   0.447466009969612,
                   0.966747552403483,
                   0.447466009969612,
                  -0.169871355636612,
                  -0.107723298696388,
                   0.046956309688169,
                   0.013810679320050,
                  -0.006905339660025}, new double[] {
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.353553390593274,
                  -0.707106781186548,
                   0.353553390593274,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000 });
            }
        }
        /// <summary>
        /// Возвращает биортогональный вейвлет 2.8.
        /// </summary>
        public static WaveletPack Bior28
        {
            get
            {
                return Create(new double[] { 
                   0.000000000000000,
                   0.001510543050630,
                  -0.003021086101261,
                  -0.012947511862547,
                   0.028916109826354,
                   0.052998481890691,
                  -0.134913073607736,
                  -0.163829183434090,
                   0.462571440475917,
                   0.951642121897179,
                   0.462571440475917,
                  -0.163829183434090,
                  -0.134913073607736,
                   0.052998481890691,
                   0.028916109826354,
                  -0.012947511862547,
                  -0.003021086101261,
                   0.001510543050630 }, new double[] {
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.353553390593274,
                  -0.707106781186548,
                   0.353553390593274,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000 });
            }
        }
        /// <summary>
        /// Возвращает биортогональный вейвлет 3.1.
        /// </summary>
        public static WaveletPack Bior31
        {
            get
            {
                return Create(new double[] {
                  -0.353553390593274,
                   1.060660171779821,
                   1.060660171779821,
                  -0.353553390593274 }, new double[] {
                  -0.176776695296637,
                   0.530330085889911,
                  -0.530330085889911,
                   0.176776695296637 });
            }
        }
        /// <summary>
        /// Возвращает биортогональный вейвлет 3.3.
        /// </summary>
        public static WaveletPack Bior33
        {
            get
            {
                return Create(new double[] {
                   0.066291260736239,
                  -0.198873782208717,
                  -0.154679608384557,
                   0.994368911043582,
                   0.994368911043582,
                  -0.154679608384557,
                  -0.198873782208717,
                   0.066291260736239 }, new double[] {
                   0.000000000000000,
                   0.000000000000000,
                  -0.176776695296637,
                   0.530330085889911,
                  -0.530330085889911,
                   0.176776695296637,
                   0.000000000000000,
                   0.000000000000000 });
            }
        }
        /// <summary>
        /// Возвращает биортогональный вейвлет 3.5.
        /// </summary>
        public static WaveletPack Bior35
        {
            get
            {
                return Create(new double[] { 
                  -0.013810679320050,
                   0.041432037960149,
                   0.052480581416189,
                  -0.267927178808965,
                  -0.071815532464259,
                   0.966747552403483,
                   0.966747552403483,
                  -0.071815532464259,
                  -0.267927178808965,
                   0.052480581416189,
                   0.041432037960149,
                  -0.013810679320050}, new double[] { 
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                  -0.176776695296637,
                   0.530330085889911,
                  -0.530330085889911,
                   0.176776695296637,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000 });
            }
        }
        /// <summary>
        /// Возвращает биортогональный вейвлет 3.7.
        /// </summary>
        public static WaveletPack Bior37
        {
            get
            {
                return Create(new double[] { 
                   0.003021086101261,
                  -0.009063258303783,
                  -0.016831765421311,
                   0.074663985074019,
                   0.031332978707363,
                  -0.301159125922835,
                  -0.026499240945345,
                   0.951642121897179,
                   0.951642121897179,
                  -0.026499240945345,
                  -0.301159125922835,
                   0.031332978707363,
                   0.074663985074019,
                  -0.016831765421311,
                  -0.009063258303783,
                   0.003021086101261 }, new double[] { 
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                  -0.176776695296637,
                   0.530330085889911,
                  -0.530330085889911,
                   0.176776695296637,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000,
                   0.000000000000000 });
            }
        }
        #endregion

        #region Daubechies wavelets
        /// <summary>
        /// Возвращает вейвлет Добеши 1-го порядка.
        /// <remarks>
        /// Вейвлет Хаара.
        /// </remarks>
        /// </summary>
        public static WaveletPack D1
        {
            get
            {
                return Create(new double[] { 
                7.071067811865475244008443621048490392848359376884740365883398e-01,
                7.071067811865475244008443621048490392848359376884740365883398e-01 
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 2-го порядка.
        /// </summary>
        public static WaveletPack D2
        {
            get
            {
                return Create(new double[] { 
                 4.829629131445341433748715998644486838169524195042022752011715e-01,
                 8.365163037378079055752937809168732034593703883484392934953414e-01,
                 2.241438680420133810259727622404003554678835181842717613871683e-01,
                -1.294095225512603811744494188120241641745344506599652569070016e-01 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 3-го порядка.
        /// </summary>
        public static WaveletPack D3
        {
            get
            {
                return Create(new double[] { 
                 3.326705529500826159985115891390056300129233992450683597084705e-01,
                 8.068915093110925764944936040887134905192973949948236181650920e-01,
                 4.598775021184915700951519421476167208081101774314923066433867e-01,
                -1.350110200102545886963899066993744805622198452237811919756862e-01,
                -8.544127388202666169281916918177331153619763898808662976351748e-02,
                 3.522629188570953660274066471551002932775838791743161039893406e-02 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 4-го порядка.
        /// </summary>
        public static WaveletPack D4
        {
            get
            {
                return Create(new double[] { 
                  2.303778133088965008632911830440708500016152482483092977910968e-01,
                  7.148465705529156470899219552739926037076084010993081758450110e-01,
                  6.308807679298589078817163383006152202032229226771951174057473e-01,
                 -2.798376941685985421141374718007538541198732022449175284003358e-02,
                 -1.870348117190930840795706727890814195845441743745800912057770e-01,
                  3.084138183556076362721936253495905017031482172003403341821219e-02,
                  3.288301166688519973540751354924438866454194113754971259727278e-02,
                 -1.059740178506903210488320852402722918109996490637641983484974e-02});
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 5-го порядка.
        /// </summary>
        public static WaveletPack D5
        {
            get
            {
                return Create(new double[] { 
                1.601023979741929144807237480204207336505441246250578327725699e-01,
                6.038292697971896705401193065250621075074221631016986987969283e-01,
                7.243085284377729277280712441022186407687562182320073725767335e-01,
                1.384281459013207315053971463390246973141057911739561022694652e-01,
               -2.422948870663820318625713794746163619914908080626185983913726e-01,
               -3.224486958463837464847975506213492831356498416379847225434268e-02,
                7.757149384004571352313048938860181980623099452012527983210146e-02,
               -6.241490212798274274190519112920192970763557165687607323417435e-03,
               -1.258075199908199946850973993177579294920459162609785020169232e-02,
                3.335725285473771277998183415817355747636524742305315099706428e-03});
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 6-го порядка.
        /// </summary>
        public static WaveletPack D6
        {
            get
            {
                return Create(new double[] {
                1.115407433501094636213239172409234390425395919844216759082360e-01,
                4.946238903984530856772041768778555886377863828962743623531834e-01,
                7.511339080210953506789344984397316855802547833382612009730420e-01,
                3.152503517091976290859896548109263966495199235172945244404163e-01,
               -2.262646939654398200763145006609034656705401539728969940143487e-01,
               -1.297668675672619355622896058765854608452337492235814701599310e-01,
                9.750160558732304910234355253812534233983074749525514279893193e-02,
                2.752286553030572862554083950419321365738758783043454321494202e-02,
               -3.158203931748602956507908069984866905747953237314842337511464e-02,
                5.538422011614961392519183980465012206110262773864964295476524e-04,
                4.777257510945510639635975246820707050230501216581434297593254e-03,
               -1.077301085308479564852621609587200035235233609334419689818580e-03});
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 7-го порядка.
        /// </summary>
        public static WaveletPack D7
        {
            get
            {
                return Create(new double[] { 
                7.785205408500917901996352195789374837918305292795568438702937e-02,
                3.965393194819173065390003909368428563587151149333287401110499e-01,
                7.291320908462351199169430703392820517179660611901363782697715e-01,
                4.697822874051931224715911609744517386817913056787359532392529e-01,
               -1.439060039285649754050683622130460017952735705499084834401753e-01,
               -2.240361849938749826381404202332509644757830896773246552665095e-01,
                7.130921926683026475087657050112904822711327451412314659575113e-02,
                8.061260915108307191292248035938190585823820965629489058139218e-02,
               -3.802993693501441357959206160185803585446196938467869898283122e-02,
               -1.657454163066688065410767489170265479204504394820713705239272e-02,
                1.255099855609984061298988603418777957289474046048710038411818e-02,
                4.295779729213665211321291228197322228235350396942409742946366e-04,
               -1.801640704047490915268262912739550962585651469641090625323864e-03,
                3.537137999745202484462958363064254310959060059520040012524275e-04});
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 8-го порядка.
        /// </summary>
        public static WaveletPack D8
        {
            get
            {
                return Create(new double[] {
                5.441584224310400995500940520299935503599554294733050397729280e-02,
                3.128715909142999706591623755057177219497319740370229185698712e-01,
                6.756307362972898068078007670471831499869115906336364227766759e-01,
                5.853546836542067127712655200450981944303266678053369055707175e-01,
               -1.582910525634930566738054787646630415774471154502826559735335e-02,
               -2.840155429615469265162031323741647324684350124871451793599204e-01,
                4.724845739132827703605900098258949861948011288770074644084096e-04,
                1.287474266204784588570292875097083843022601575556488795577000e-01,
               -1.736930100180754616961614886809598311413086529488394316977315e-02,
               -4.408825393079475150676372323896350189751839190110996472750391e-02,
                1.398102791739828164872293057263345144239559532934347169146368e-02,
                8.746094047405776716382743246475640180402147081140676742686747e-03,
               -4.870352993451574310422181557109824016634978512157003764736208e-03,
               -3.917403733769470462980803573237762675229350073890493724492694e-04,
                6.754494064505693663695475738792991218489630013558432103617077e-04,
               -1.174767841247695337306282316988909444086693950311503927620013e-04 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 9-го порядка.
        /// </summary>
        public static WaveletPack D9
        {
            get
            {
                return Create(new double[] { 
                3.807794736387834658869765887955118448771714496278417476647192e-02,
                2.438346746125903537320415816492844155263611085609231361429088e-01,
                6.048231236901111119030768674342361708959562711896117565333713e-01,
                6.572880780513005380782126390451732140305858669245918854436034e-01,
                1.331973858250075761909549458997955536921780768433661136154346e-01,
               -2.932737832791749088064031952421987310438961628589906825725112e-01,
               -9.684078322297646051350813353769660224825458104599099679471267e-02,
                1.485407493381063801350727175060423024791258577280603060771649e-01,
                3.072568147933337921231740072037882714105805024670744781503060e-02,
               -6.763282906132997367564227482971901592578790871353739900748331e-02,
                2.509471148314519575871897499885543315176271993709633321834164e-04,
                2.236166212367909720537378270269095241855646688308853754721816e-02,
               -4.723204757751397277925707848242465405729514912627938018758526e-03,
               -4.281503682463429834496795002314531876481181811463288374860455e-03,
                1.847646883056226476619129491125677051121081359600318160732515e-03,
                2.303857635231959672052163928245421692940662052463711972260006e-04,
               -2.519631889427101369749886842878606607282181543478028214134265e-04,
                3.934732031627159948068988306589150707782477055517013507359938e-05 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 10-го порядка.
        /// </summary>
        public static WaveletPack D10
        {
            get
            {
                return Create(new double[] {
                2.667005790055555358661744877130858277192498290851289932779975e-02,
                1.881768000776914890208929736790939942702546758640393484348595e-01,
                5.272011889317255864817448279595081924981402680840223445318549e-01,
                6.884590394536035657418717825492358539771364042407339537279681e-01,
                2.811723436605774607487269984455892876243888859026150413831543e-01,
               -2.498464243273153794161018979207791000564669737132073715013121e-01,
               -1.959462743773770435042992543190981318766776476382778474396781e-01,
                1.273693403357932600826772332014009770786177480422245995563097e-01,
                9.305736460357235116035228983545273226942917998946925868063974e-02,
               -7.139414716639708714533609307605064767292611983702150917523756e-02,
               -2.945753682187581285828323760141839199388200516064948779769654e-02,
                3.321267405934100173976365318215912897978337413267096043323351e-02,
                3.606553566956169655423291417133403299517350518618994762730612e-03,
               -1.073317548333057504431811410651364448111548781143923213370333e-02,
                1.395351747052901165789318447957707567660542855688552426721117e-03,
                1.992405295185056117158742242640643211762555365514105280067936e-03,
               -6.858566949597116265613709819265714196625043336786920516211903e-04,
               -1.164668551292854509514809710258991891527461854347597362819235e-04,
                9.358867032006959133405013034222854399688456215297276443521873e-05,
               -1.326420289452124481243667531226683305749240960605829756400674e-05
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 11-го порядка.
        /// </summary>
        public static WaveletPack D11
        {
            get
            {
                return Create(new double[] {
                 1.869429776147108402543572939561975728967774455921958543286692e-02,
                 1.440670211506245127951915849361001143023718967556239604318852e-01,
                 4.498997643560453347688940373853603677806895378648933474599655e-01,
                 6.856867749162005111209386316963097935940204964567703495051589e-01,
                 4.119643689479074629259396485710667307430400410187845315697242e-01,
                -1.622752450274903622405827269985511540744264324212130209649667e-01,
                -2.742308468179469612021009452835266628648089521775178221905778e-01,
                 6.604358819668319190061457888126302656753142168940791541113457e-02,
                 1.498120124663784964066562617044193298588272420267484653796909e-01,
                -4.647995511668418727161722589023744577223260966848260747450320e-02,
                -6.643878569502520527899215536971203191819566896079739622858574e-02,
                 3.133509021904607603094798408303144536358105680880031964936445e-02,
                 2.084090436018106302294811255656491015157761832734715691126692e-02,
                -1.536482090620159942619811609958822744014326495773000120205848e-02,
                -3.340858873014445606090808617982406101930658359499190845656731e-03,
                 4.928417656059041123170739741708273690285547729915802418397458e-03,
                -3.085928588151431651754590726278953307180216605078488581921562e-04,
                -8.930232506662646133900824622648653989879519878620728793133358e-04,
                 2.491525235528234988712216872666801088221199302855425381971392e-04,
                 5.443907469936847167357856879576832191936678525600793978043688e-05,
                -3.463498418698499554128085159974043214506488048233458035943601e-05,
                 4.494274277236510095415648282310130916410497987383753460571741e-06
});
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 12-го порядка.
        /// </summary>
        public static WaveletPack D12
        {
            get
            {
                return Create(new double[] {
                 1.311225795722951750674609088893328065665510641931325007748280e-02,
                 1.095662728211851546057045050248905426075680503066774046383657e-01,
                 3.773551352142126570928212604879206149010941706057526334705839e-01,
                 6.571987225793070893027611286641169834250203289988412141394281e-01,
                 5.158864784278156087560326480543032700677693087036090056127647e-01,
                -4.476388565377462666762747311540166529284543631505924139071704e-02,
                -3.161784537527855368648029353478031098508839032547364389574203e-01,
                -2.377925725606972768399754609133225784553366558331741152482612e-02,
                 1.824786059275796798540436116189241710294771448096302698329011e-01,
                 5.359569674352150328276276729768332288862665184192705821636342e-03,
                -9.643212009650708202650320534322484127430880143045220514346402e-02,
                 1.084913025582218438089010237748152188661630567603334659322512e-02,
                 4.154627749508444073927094681906574864513532221388374861287078e-02,
                -1.221864906974828071998798266471567712982466093116558175344811e-02,
                -1.284082519830068329466034471894728496206109832314097633275225e-02,
                 6.711499008795509177767027068215672450648112185856456740379455e-03,
                 2.248607240995237599950865211267234018343199786146177099262010e-03,
                -2.179503618627760471598903379584171187840075291860571264980942e-03,
                 6.545128212509595566500430399327110729111770568897356630714552e-06,
                 3.886530628209314435897288837795981791917488573420177523436096e-04,
                -8.850410920820432420821645961553726598738322151471932808015443e-05,
                -2.424154575703078402978915320531719580423778362664282239377532e-05,
                 1.277695221937976658714046362616620887375960941439428756055353e-05,
                -1.529071758068510902712239164522901223197615439660340672602696e-06
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 13-го порядка.
        /// </summary>
        public static WaveletPack D13
        {
            get
            {
                return Create(new double[] {
                 9.202133538962367972970163475644184667534171916416562386009703e-03,
                 8.286124387290277964432027131230466405208113332890135072514277e-02,
                 3.119963221604380633960784112214049693946683528967180317160390e-01,
                 6.110558511587876528211995136744180562073612676018239438526582e-01,
                 5.888895704312189080710395347395333927665986382812836042235573e-01,
                 8.698572617964723731023739838087494399231884076619701250882016e-02,
                -3.149729077113886329981698255932282582876888450678789025950306e-01,
                -1.245767307508152589413808336021260180792739295173634719572069e-01,
                 1.794760794293398432348450072339369013581966256244133393042881e-01,
                 7.294893365677716380902830610477661983325929026879873553627963e-02,
                -1.058076181879343264509667304196464849478860754801236658232360e-01,
                -2.648840647534369463963912248034785726419604844297697016264224e-02,
                 5.613947710028342886214501998387331119988378792543100244737056e-02,
                 2.379972254059078811465170958554208358094394612051934868475139e-03,
                -2.383142071032364903206403067757739134252922717636226274077298e-02,
                 3.923941448797416243316370220815526558824746623451404043918407e-03,
                 7.255589401617566194518393300502698898973529679646683695269828e-03,
                -2.761911234656862178014576266098445995350093330501818024966316e-03,
                -1.315673911892298936613835370593643376060412592653652307238124e-03,
                 9.323261308672633862226517802548514100918088299801952307991569e-04,
                 4.925152512628946192140957387866596210103778299388823500840094e-05,
                -1.651289885565054894616687709238000755898548214659776703347801e-04,
                 3.067853757932549346649483228575476236600428217237900563128230e-05,
                 1.044193057140813708170714991080596951670706436217328169641474e-05,
                -4.700416479360868325650195165061771321650383582970958556568059e-06,
                5.220035098454864691736424354843176976747052155243557001531901e-07
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 14-го порядка.
        /// </summary>
        public static WaveletPack D14
        {
            get
            {
                return Create(new double[]
                {
                     6.461153460087947818166397448622814272327159419201199218101404e-03,
                     6.236475884939889832798566758434877428305333693407667164602518e-02,
                     2.548502677926213536659077886778286686187042416367137443780084e-01,
                     5.543056179408938359926831449851154844078269830951634609683997e-01,
                     6.311878491048567795576617135358172348623952456570017289788809e-01,
                     2.186706877589065214917475918217517051765774321270432059030273e-01,
                    -2.716885522787480414142192476181171094604882465683330814311896e-01,
                    -2.180335299932760447555558812702311911975240669470604752747127e-01,
                     1.383952138648065910739939690021573713989900463229686119059119e-01,
                     1.399890165844607012492943162271163440328221555614326181333683e-01,
                    -8.674841156816968904560822066727795382979149539517503657492964e-02,
                    -7.154895550404613073584145115173807990958069673129538099990913e-02,
                     5.523712625921604411618834060533403397913833632511672157671107e-02,
                     2.698140830791291697399031403215193343375766595807274233284349e-02,
                    -3.018535154039063518714822623489137573781575406658652624883756e-02,
                    -5.615049530356959133218371367691498637457297203925810387698680e-03,
                     1.278949326633340896157330705784079299374903861572058313481534e-02,
                    -7.462189892683849371817160739181780971958187988813302900435487e-04,
                    -3.849638868022187445786349316095551774096818508285700493058915e-03,
                     1.061691085606761843032566749388411173033941582147830863893939e-03,
                     7.080211542355278586442977697617128983471863464181595371670094e-04,
                    -3.868319473129544821076663398057314427328902107842165379901468e-04,
                    -4.177724577037259735267979539839258928389726590132730131054323e-05,
                     6.875504252697509603873437021628031601890370687651875279882727e-05,
                    -1.033720918457077394661407342594814586269272509490744850691443e-05,
                    -4.389704901781394115254042561367169829323085360800825718151049e-06,
                     1.724994675367812769885712692741798523587894709867356576910717e-06,
                    -1.787139968311359076334192938470839343882990309976959446994022e-07
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 15-го порядка.
        /// </summary>
        public static WaveletPack D15
        {
            get
            {
                return Create(new double[]
                {
                     4.538537361578898881459394910211696346663671243788786997916513e-03,
                     4.674339489276627189170969334843575776579151700214943513113197e-02,
                     2.060238639869957315398915009476307219306138505641930902702047e-01,
                     4.926317717081396236067757074029946372617221565130932402160160e-01,
                     6.458131403574243581764209120106917996432608287494046181071489e-01,
                     3.390025354547315276912641143835773918756769491793554669336690e-01,
                    -1.932041396091454287063990534321471746304090039142863827937754e-01,
                    -2.888825965669656462484125009822332981311435630435342594971292e-01,
                     6.528295284877281692283107919869574882039174285596144125965101e-02,
                     1.901467140071229823484893116586020517959501258174336696878156e-01,
                    -3.966617655579094448384366751896200668381742820683736805449745e-02,
                    -1.111209360372316933656710324674058608858623762165914120505657e-01,
                     3.387714392350768620854817844433523770864744687411265369463195e-02,
                     5.478055058450761268913790312581879108609415997422768564244845e-02,
                    -2.576700732843996258594525754269826392203641634825340138396836e-02,
                    -2.081005016969308167788483424677000162054657951364899040996166e-02,
                     1.508391802783590236329274460170322736244892823305627716233968e-02,
                     5.101000360407543169708860185565314724801066527344222055526631e-03,
                    -6.487734560315744995181683149218690816955845639388826407928967e-03,
                    -2.417564907616242811667225326300179605229946995814535223329411e-04,
                     1.943323980382211541764912332541087441011424865579531401452302e-03,
                    -3.734823541376169920098094213645414611387630968030256625740226e-04,
                    -3.595652443624688121649620075909808858194202454084090305627480e-04,
                     1.558964899205997479471658241227108816255567059625495915228603e-04,
                     2.579269915531893680925862417616855912944042368767340709160119e-05,
                    -2.813329626604781364755324777078478665791443876293788904267255e-05,
                     3.362987181737579803124845210420177472134846655864078187186304e-06,
                     1.811270407940577083768510912285841160577085925337507850590290e-06,
                    -6.316882325881664421201597299517657654166137915121195510416641e-07,
                    6.133359913305752029056299460289788601989190450885396512173845e-08
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 16-го порядка.
        /// </summary>
        public static WaveletPack D16
        {
            get
            {
                return Create(new double[]
                {
                     3.189220925347738029769547564645958687067086750131428767875878e-03,
                     3.490771432367334641030147224023020009218241430503984146140054e-02,
                     1.650642834888531178991252730561134811584835002342723240213592e-01,
                     4.303127228460038137403925424357684620633970478036986773924646e-01,
                     6.373563320837888986319852412996030536498595940814198125967751e-01,
                     4.402902568863569000390869163571679288527803035135272578789884e-01,
                    -8.975108940248964285718718077442597430659247445582660149624718e-02,
                    -3.270633105279177046462905675689119641757228918228812428141723e-01,
                    -2.791820813302827668264519595026873204339971219174736041535479e-02,
                     2.111906939471042887209680163268837900928491426167679439251042e-01,
                     2.734026375271604136485245757201617965429027819507130220231500e-02,
                    -1.323883055638103904500474147756493375092287817706027978798549e-01,
                    -6.239722752474871765674503394120025865444656311678760990761458e-03,
                     7.592423604427631582148498743941422461530405946100943351940313e-02,
                    -7.588974368857737638494890864636995796586975144990925400097160e-03,
                    -3.688839769173014233352666320894554314718748429706730831064068e-02,
                     1.029765964095596941165000580076616900528856265803662208854147e-02,
                     1.399376885982873102950451873670329726409840291727868988490100e-02,
                    -6.990014563413916670284249536517288338057856199646469078115759e-03,
                    -3.644279621498389932169000540933629387055333973353108668841215e-03,
                     3.128023381206268831661202559854678767821471906193608117450360e-03,
                     4.078969808497128362417470323406095782431952972310546715071397e-04,
                    -9.410217493595675889266453953635875407754747216734480509250273e-04,
                     1.142415200387223926440228099555662945839684344936472652877091e-04,
                     1.747872452253381803801758637660746874986024728615399897971953e-04,
                    -6.103596621410935835162369150522212811957259981965919143961722e-05,
                    -1.394566898820889345199078311998401982325273569198675335408707e-05,
                     1.133660866127625858758848762886536997519471068203753661757843e-05,
                    -1.043571342311606501525454737262615404887478930635676471546032e-06,
                    -7.363656785451205512099695719725563646585445545841663327433569e-07,
                     2.308784086857545866405412732942006121306306735866655525372544e-07,
                    -2.109339630100743097000572623603489906836297584591605307745349e-08
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 17-го порядка.
        /// </summary>
        public static WaveletPack D17
        {
            get
            {
                return Create(new double[]
                {
                     2.241807001037312853535962677074436914062191880560370733250531e-03,
                     2.598539370360604338914864591720788315473944524878241294399948e-02,
                     1.312149033078244065775506231859069960144293609259978530067004e-01,
                     3.703507241526411504492548190721886449477078876896803823650425e-01,
                     6.109966156846228181886678867679372082737093893358726291371783e-01,
                     5.183157640569378393254538528085968046216817197718416402439904e-01,
                     2.731497040329363500431250719147586480350469818964563003672942e-02,
                    -3.283207483639617360909665340725061767581597698151558024679130e-01,
                    -1.265997522158827028744679110933825505053966260104086162103728e-01,
                     1.973105895650109927854047044781930142551422414135646917122284e-01,
                     1.011354891774702721509699856433434802196622545499664876109437e-01,
                    -1.268156917782863110948571128662331680384792185915017065732137e-01,
                    -5.709141963167692728911239478651382324161160869845347053990144e-02,
                     8.110598665416088507965885748555429201024364190954499194020678e-02,
                     2.231233617810379595339136059534813756232242114093689244020869e-02,
                    -4.692243838926973733300897059211400507138768125498030602878439e-02,
                    -3.270955535819293781655360222177494452069525958061609392809275e-03,
                     2.273367658394627031845616244788448969906713741338339498024864e-02,
                    -3.042989981354637068592482637907206078633395457225096588287881e-03,
                    -8.602921520322854831713706413243659917926736284271730611920986e-03,
                     2.967996691526094872806485060008038269959463846548378995044195e-03,
                     2.301205242153545624302059869038423604241976680189447476064764e-03,
                    -1.436845304802976126222890402980384903503674530729935809561434e-03,
                    -3.281325194098379713954444017520115075812402442728749700195651e-04,
                     4.394654277686436778385677527317841632289249319738892179465910e-04,
                    -2.561010956654845882729891210949920221664082061531909655178413e-05,
                    -8.204803202453391839095482576282189866136273049636764338689593e-05,
                     2.318681379874595084482068205706277572106695174091895338530734e-05,
                     6.990600985076751273204549700855378627762758585902057964027481e-06,
                    -4.505942477222988194102268206378312129713572600716499944918416e-06,
                     3.016549609994557415605207594879939763476168705217646897702706e-07,
                     2.957700933316856754979905258816151367870345628924317307354639e-07,
                    -8.423948446002680178787071296922877068410310942222799622593133e-08,
                    7.267492968561608110879767441409035034158581719789791088892046e-09
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 18-го порядка.
        /// </summary>
        public static WaveletPack D18
        {
            get
            {
                return Create(new double[]
                {
                     1.576310218440760431540744929939777747670753710991660363684429e-03,
                     1.928853172414637705921391715829052419954667025288497572236714e-02,
                     1.035884658224235962241910491937253596470696555220241672976224e-01,
                     3.146789413370316990571998255652579931786706190489374509491307e-01,
                     5.718268077666072234818589370900623419393673743130930561295324e-01,
                     5.718016548886513352891119994065965025668047882818525060759395e-01,
                     1.472231119699281415750977271081072312557864107355701387801677e-01,
                    -2.936540407365587442479030994981150723935710729035053239661752e-01,
                    -2.164809340051429711237678625668271471437937235669492408388692e-01,
                     1.495339755653777893509301738913667208804816691893765610261943e-01,
                     1.670813127632574045149318139950134745324205646353988083152250e-01,
                    -9.233188415084628060429372558659459731431848000144569612074508e-02,
                    -1.067522466598284855932200581614984861385266404624112083917702e-01,
                     6.488721621190544281947577955141911463129382116634147846137149e-02,
                     5.705124773853688412090768846499622260596226120431038524600676e-02,
                    -4.452614190298232471556143559744653492971477891439833592755034e-02,
                    -2.373321039586000103275209582665216110197519330713490233071565e-02,
                     2.667070592647059029987908631672020343207895999936072813363471e-02,
                     6.262167954305707485236093144497882501990325204745013190268052e-03,
                    -1.305148094661200177277636447600807169755191054507571666606133e-02,
                     1.186300338581174657301741592161819084544899417452317405185615e-04,
                     4.943343605466738130665529516802974834299638313366477765295203e-03,
                    -1.118732666992497072800658855238650182318060482584970145512687e-03,
                    -1.340596298336106629517567228251583609823044524685986640323942e-03,
                     6.284656829651457125619449885420838217551022796301582874349652e-04,
                     2.135815619103406884039052814341926025873200325996466522543440e-04,
                    -1.986485523117479485798245416362489554927797880264017876139605e-04,
                    -1.535917123534724675069770335876717193700472427021513236587288e-07,
                     3.741237880740038181092208138035393952304292615793985030731363e-05,
                    -8.520602537446695203919254911655523022437596956226376512305917e-06,
                    -3.332634478885821888782452033341036827311505907796498439829337e-06,
                     1.768712983627615455876328730755375176412501359114058815453100e-06,
                    -7.691632689885176146000152878539598405817397588156525116769908e-08,
                    -1.176098767028231698450982356561292561347579777695396953528141e-07,
                     3.068835863045174800935478294933975372450179787894574492930570e-08,
                    -2.507934454948598267195173183147126731806317144868275819941403e-09
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 19-го порядка.
        /// </summary>
        public static WaveletPack D19
        {
            get
            {
                return Create(new double[]
                {
                     1.108669763181710571099154195209715164245299677773435932135455e-03,
                     1.428109845076439737439889152950199234745663442163665957870715e-02,
                     8.127811326545955065296306784901624839844979971028620366497726e-02,
                     2.643884317408967846748100380289426873862377807211920718417385e-01,
                     5.244363774646549153360575975484064626044633641048072116393160e-01,
                     6.017045491275378948867077135921802620536565639585963293313931e-01,
                     2.608949526510388292872456675310528324172673101301907739925213e-01,
                    -2.280913942154826463746325776054637207093787237086425909534822e-01,
                    -2.858386317558262418545975695028984237217356095588335149922119e-01,
                     7.465226970810326636763433111878819005865866149731909656365399e-02,
                     2.123497433062784888090608567059824197077074200878839448416908e-01,
                    -3.351854190230287868169388418785731506977845075238966819814032e-02,
                    -1.427856950387365749779602731626112812998497706152428508627562e-01,
                     2.758435062562866875014743520162198655374474596963423080762818e-02,
                     8.690675555581223248847645428808443034785208002468192759640352e-02,
                    -2.650123625012304089901835843676387361075068017686747808171345e-02,
                    -4.567422627723090805645444214295796017938935732115630050880109e-02,
                     2.162376740958504713032984257172372354318097067858752542571020e-02,
                     1.937554988917612764637094354457999814496885095875825546406963e-02,
                    -1.398838867853514163250401235248662521916813867453095836808366e-02,
                    -5.866922281012174726584493436054373773814608340808758177372765e-03,
                     7.040747367105243153014511207400620109401689897665383078229398e-03,
                     7.689543592575483559749139148673955163477947086039406129546422e-04,
                    -2.687551800701582003957363855070398636534038920982478290170267e-03,
                     3.418086534585957765651657290463808135214214848819517257794031e-04,
                     7.358025205054352070260481905397281875183175792779904858189494e-04,
                    -2.606761356786280057318315130897522790383939362073563408613547e-04,
                    -1.246007917341587753449784408901653990317341413341980904757592e-04,
                     8.711270467219922965416862388191128268412933893282083517729443e-05,
                     5.105950487073886053049222809934231573687367992106282669389264e-06,
                    -1.664017629715494454620677719899198630333675608812018108739144e-05,
                     3.010964316296526339695334454725943632645798938162427168851382e-06,
                     1.531931476691193063931832381086636031203123032723477463624141e-06,
                    -6.862755657769142701883554613486732854452740752771392411758418e-07,
                     1.447088298797844542078219863291615420551673574071367834316167e-08,
                     4.636937775782604223430857728210948898871748291085962296649320e-08,
                    -1.116402067035825816390504769142472586464975799284473682246076e-08,
                    8.666848838997619350323013540782124627289742190273059319122840e-10
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 20-го порядка.
        /// </summary>
        public static WaveletPack D20
        {
            get
            {
                return Create(new double[]
                {
                     7.799536136668463215861994818889370970510722039232863880031127e-04,
                     1.054939462495039832454480973015641498231961468733236691299796e-02,
                     6.342378045908151497587346582668785136406523315729666353643372e-02,
                     2.199421135513970450080335972537209392121306761010882209298252e-01,
                     4.726961853109016963710241465101446230757804141171727845834637e-01,
                     6.104932389385938201631515660084201906858628924695448898824748e-01,
                     3.615022987393310629195602665268631744967084723079677894136358e-01,
                    -1.392120880114838725806970545155530518264944915437808314813582e-01,
                    -3.267868004340349674031122837905370666716645587480021744425550e-01,
                    -1.672708830907700757517174997304297054003744303620479394006890e-02,
                     2.282910508199163229728429126648223086437547237250290835639880e-01,
                     3.985024645777120219790581076522174181104027576954427684456660e-02,
                    -1.554587507072679559315307870562464374359996091752285157077477e-01,
                    -2.471682733861358401587992299169922262915151413349313513685587e-02,
                     1.022917191744425578861013681016866083888381385233081516583444e-01,
                     5.632246857307435506953246988215209861566800664402785938591145e-03,
                    -6.172289962468045973318658334083283558209278762007041823250642e-02,
                     5.874681811811826491300679742081997167209743446956901841959711e-03,
                     3.229429953076958175885440860617219117564558605035979601073235e-02,
                    -8.789324923901561348753650366700695916503030939283830968151332e-03,
                    -1.381052613715192007819606423860356590496904285724730356602106e-02,
                     6.721627302259456835336850521405425560520025237915708362002910e-03,
                     4.420542387045790963058229526673514088808999478115581153468068e-03,
                    -3.581494259609622777556169638358238375765194248623891034940330e-03,
                    -8.315621728225569192482585199373230956924484221135739973390038e-04,
                     1.392559619323136323905254999347967283760544147397530531142397e-03,
                    -5.349759843997695051759716377213680036185796059087353172073952e-05,
                    -3.851047486992176060650288501475716463266233035937022303649838e-04,
                     1.015328897367029050797488785306056522529979267572003990901472e-04,
                     6.774280828377729558011184406727978221295796652200819839464354e-05,
                    -3.710586183394712864227221271216408416958225264980612822617745e-05,
                    -4.376143862183996810373095822528607606900620592585762190542483e-06,
                     7.241248287673620102843105877497181565468725757387007139555885e-06,
                    -1.011994010018886150340475413756849103197395069431085005709201e-06,
                    -6.847079597000556894163334787575159759109091330092963990364192e-07,
                     2.633924226270001084129057791994367121555769686616747162262697e-07,
                     2.014322023550512694324757845944026047904414136633776958392681e-10,
                    -1.814843248299695973210605258227024081458531110762083371310917e-08,
                     4.056127055551832766099146230616888024627380574113178257963252e-09,
                    -2.998836489619319566407767078372705385732460052685621923178375e-10
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 21-го порядка.
        /// </summary>
        public static WaveletPack D21
        {
            get
            {
                return Create(new double[]
                {
                     5.488225098526837086776336675992521426750673054588245523834775e-04,
                     7.776639052354783754338787398088799862510779059555623704879234e-03,
                     4.924777153817727491399853378340056968104483161598320693657954e-02,
                     1.813596254403815156260378722764624190931951510708050516519181e-01,
                     4.196879449393627730946850609089266339973601543036294871772653e-01,
                     6.015060949350038975629880664020955953066542593896126705346122e-01,
                     4.445904519276003403643290994523601016151342743089878478478962e-01,
                    -3.572291961725529045922914178005307189036762547143966578066838e-02,
                    -3.356640895305295094832978867114363069987575282256098351499731e-01,
                    -1.123970715684509813515004981340306901641824212464197973490295e-01,
                     2.115645276808723923846781645238468659430862736248896128529373e-01,
                     1.152332984396871041993434411681730428103160016594558944687967e-01,
                    -1.399404249325472249247758764839776903226503657502071670245304e-01,
                    -8.177594298086382887387303634193790542522570670234556157566786e-02,
                     9.660039032372422070232189700372539681627783322249829842275517e-02,
                     4.572340574922879239251202944731235421034828710753381191345186e-02,
                    -6.497750489373232063332311106008616685748929419452249544690967e-02,
                    -1.865385920211851534093244412008141266131208093007217139232170e-02,
                     3.972683542785044175197464400756126818299918992482587866999707e-02,
                     3.357756390338110842532604766376200760791669954106679933144723e-03,
                    -2.089205367797907948785235479746212371728219866525211135343707e-02,
                     2.403470920805434762380632169785689545910525667396313550679652e-03,
                     8.988824381971911875349463398395464114417817949738911101372312e-03,
                    -2.891334348588901247375268718015882610844675931117463495551958e-03,
                    -2.958374038932831280750770228215510959830170264176955719827510e-03,
                     1.716607040630624138494506282569230126333308533535502799235333e-03,
                     6.394185005120302146432543767052865436099994387647359452249347e-04,
                    -6.906711170821016507268939228893784790518270744313525548714065e-04,
                    -3.196406277680437193708834220804640347636984901270948088339102e-05,
                     1.936646504165080615323696689856004910579777568504218782029027e-04,
                    -3.635520250086338309442855006186370752206331429871136596927137e-05,
                    -3.499665984987447953974079490046597240276268044409625722689849e-05,
                     1.535482509276049283124233498646050472096482329299719141107128e-05,
                     2.790330539814487046106169582691767916283793946025922387556917e-06,
                    -3.090017164545699197158555936852697325985864588418167982685400e-06,
                     3.166095442367030556603889009833954440058545355777781782000278e-07,
                     2.992136630464852794401294607536813682771292352506328096125857e-07,
                    -1.000400879030597332045460600516621971679363965166249211063755e-07,
                    -2.254014974673330131563184851456825991617915549643308754828159e-09,
                     7.058033541231121859020947976903904685464512825731230495144226e-09,
                    -1.471954197650365265189549600816698778213247061389470277337173e-09,
                    1.038805571023706553035373138760372703492942617518816122570050e-10
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 22-го порядка.
        /// </summary>
        public static WaveletPack D22
        {
            get
            {
                return Create(new double[]
                {
                     3.862632314910982158524358900615460368877852009576899680767316e-04,
                     5.721854631334539120809783403484493333555361591386208129183833e-03,
                     3.806993723641108494769873046391825574447727068953448390456335e-02,
                     1.483675408901114285014404448710249837385836373168215616427030e-01,
                     3.677286834460374788614690818452372827430535649696462720334897e-01,
                     5.784327310095244271421181831735444106385099957908657145590104e-01,
                     5.079010906221639018391523325390716836568713192498711562711282e-01,
                     7.372450118363015165570139016530653113725172412104955350368114e-02,
                    -3.127265804282961918033226222621788537078452535993545440716988e-01,
                    -2.005684061048870939324361244042200174132905844868237447130382e-01,
                     1.640931881067664818606223226286885712554385317412228836705888e-01,
                     1.799731879928913037252154295313083168387840791424988422757762e-01,
                    -9.711079840911470969274209179691733251456735137994201552926799e-02,
                    -1.317681376866834107513648518146838345477875022352088357523838e-01,
                     6.807631439273221556739202147004580559367442550641388181886023e-02,
                     8.455737636682607503362813659356786494357635805197410905877078e-02,
                    -5.136425429744413245727949984018884707909441768477091944584584e-02,
                    -4.653081182750671347875833607846979997825771277976548080904423e-02,
                     3.697084662069802057615318892988581825637896696876361343354380e-02,
                     2.058670762756536044060249710676656807281671451609632981487139e-02,
                    -2.348000134449318868560142854519364987363882333754753819791381e-02,
                    -6.213782849364658499069336123807608293122900450508440420104462e-03,
                     1.256472521834337406887017835495604463815382993214296088172221e-02,
                     3.001373985076435951229129255588255746904937042979316054485183e-04,
                    -5.455691986156717076595353163071679107868762395367234726592273e-03,
                     1.044260739186025323350755659184734060807432172611689413745029e-03,
                     1.827010495657279080112597436850157110235336772062961041154607e-03,
                    -7.706909881231196232880372722955519781655769913634565757339739e-04,
                    -4.237873998391800799531947768003976978197438302533528661825758e-04,
                     3.286094142136787341983758471405935405823323072829619248523697e-04,
                     4.345899904532003379046992625575076092823809665933575578710696e-05,
                    -9.405223634815760421845190098352673647881298980040512091599943e-05,
                     1.137434966212593172736144274866639210339820203135670505287250e-05,
                     1.737375695756189356163565074505405906859746605867772002320509e-05,
                    -6.166729316467578372152251668422979152169587307212708981768966e-06,
                    -1.565179131995160159307426993578204733378112742579926503832095e-06,
                     1.295182057318877573889711232345068147800395721925682566394936e-06,
                    -8.779879873361286276888117046153049053917243760475816789226764e-08,
                    -1.283336228751754417819693932114064887075096030264748079976736e-07,
                     3.761228749337362366156711648187743399164239397803629022612862e-08,
                     1.680171404922988885554331183691280245962290247654438114807112e-09,
                    -2.729623146632976083449327361739104754443221903317745768938846e-09,
                     5.335938821667489905169783227036804533253011117886586305435615e-10,
                    -3.602113484339554703794807810939301847299106970237814334104274e-11
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 23-го порядка.
        /// </summary>
        public static WaveletPack D23
        {
            get
            {
                return Create(new double[]
                {
                     2.719041941282888414192673609703302357098336003920923958924757e-04,
                     4.202748893183833538390034372523511472345215563611003407984701e-03,
                     2.931000365788411514736204018929480427874317460676079959515131e-02,
                     1.205155317839719336306053895611899089004274336891709067958035e-01,
                     3.184508138528652363416527748460472152790575031409830417259640e-01,
                     5.449311478735204282674240672421984387504149924834544495466793e-01,
                     5.510185172419193913452724227212507720514144116478727269717859e-01,
                     1.813926253638400136259098302138614937264260737638175539416540e-01,
                    -2.613921480306441118856795735210118413900307577511142987337375e-01,
                    -2.714020986078430556604069575184718123763697177381058877113471e-01,
                     9.212540708241805260646030910734894258577648089100630012130261e-02,
                     2.235736582420402317149513960822561717689875252792817094811874e-01,
                    -3.303744709428937875006612792463031409461636228731285046551636e-02,
                    -1.640113215318759250156057837165276039181451149292112929401186e-01,
                     2.028307457564929974897286607551313323418860610791382310375731e-02,
                     1.122970436181072886950734465075645977754665593869789965874572e-01,
                    -2.112621235622724100704783293549467048999443844657058425212982e-02,
                    -7.020739157490110946204219011957565343899895499962369353294028e-02,
                     2.176585683449997560776882472168730165799461445156766923497545e-02,
                     3.849533252256919901057154320407596073180564628069920893870768e-02,
                    -1.852351365015615979794689960740674782817814176166333519597796e-02,
                    -1.753710100303584537915846117408613551147985251726558719415169e-02,
                     1.275194393152828646243157404474947115052750581861997731041018e-02,
                     6.031840650024162816289878206037841640814102314209075233751820e-03,
                    -7.075319273706152814194039481466556204493276773483821748740018e-03,
                    -1.134865473356251691289337120013286756337393784110786907825400e-03,
                     3.122876449818144997419144765125750522437659393621577492535411e-03,
                    -2.465014005163512031940473100375377210862560761576109755841161e-04,
                    -1.061231228886651321139357625683805642193648671030425010215075e-03,
                     3.194204927099011503676530359692366990929679170022583007683112e-04,
                     2.567624520078737205563856675376636092314813400664190770435450e-04,
                    -1.500218503490340967673163290447832236259277810659068637402668e-04,
                    -3.378894834120903434270962452674534330903724108906662510305045e-05,
                     4.426071203109246077621875303440935335701832843654692827539837e-05,
                    -2.635207889249186237209225933170897825432335273771458456888097e-06,
                    -8.347875567854625544366043748844183086765894974439245409223337e-06,
                     2.397569546840240057403739507525641239509517148980849889986407e-06,
                     8.147574834779447778085443041422881439860288287528356019216814e-07,
                    -5.339005405209421154584783682848780965053642859373536945701365e-07,
                     1.853091785633965019353699857864654181728710556702529908304185e-08,
                     5.417549179539278736503176166323685597634496102979977037271945e-08,
                    -1.399935495437998845130909687361847103274208993447892120341999e-08,
                    -9.472885901812050535221582074673490573092096712822067564903012e-10,
                     1.050446453696543404071105111096438573423068913105255997908040e-09,
                    -1.932405111313417542192651899622541612314066389643607507706887e-10,
                    1.250203302351040941433216718217504240541423430995137507404787e-11
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 24-го порядка.
        /// </summary>
        public static WaveletPack D24
        {
            get
            {
                return Create(new double[]
                {
                     1.914358009475513695026138336474115599435172088053846745168462e-04,
                     3.082081714905494436206199424544404720984720556128685270556458e-03,
                     2.248233994971641072358415157184825628226776692231940577581580e-02,
                     9.726223583362519663806545734008355914527504417674578571164300e-02,
                     2.729089160677263268706137134412557268751671263458895098625356e-01,
                     5.043710408399249919771876890402814109246866444441814540282099e-01,
                     5.749392210955419968460807901923407033144945935105622912839838e-01,
                     2.809855532337118833442626085115402941842959475929278883281409e-01,
                    -1.872714068851562376981887159775791469060265778441667840307934e-01,
                    -3.179430789993627375453948489797707550898087789160025182664299e-01,
                     4.776613684344728187950198323031360866349104994035553200788631e-03,
                     2.392373887803108551973268291945824822214858134512317715815616e-01,
                     4.252872964148383258147364472170645232684343235486951540533893e-02,
                    -1.711753513703468896897638515080572393949165942335556397917666e-01,
                    -3.877717357792001620177594726199572688446488033750771020190283e-02,
                     1.210163034692242362312637311149062286659377039046006801523826e-01,
                     2.098011370914481534980883827326017063121637262728447783605518e-02,
                    -8.216165420800166702291466006164189460916816748629968198028898e-02,
                    -4.578436241819221637997516339765068825260159169893967894877272e-03,
                     5.130162003998087915555334881398688958843078494595140394873884e-02,
                    -4.944709428125628299815920032649550811877887219282751174798211e-03,
                    -2.821310709490189098113895361900699228886900995412759197674058e-02,
                     7.661721881646585897329899904308764405384658404613669817843430e-03,
                     1.304997087108573583052494067883717533043101857128653233783396e-02,
                    -6.291435370018187780721843581169343900864298634085743861509767e-03,
                    -4.746568786323113800477796959513558401732252800905982385017245e-03,
                     3.736046178282523345179052160810332868725126356493155728625572e-03,
                     1.153764936839481504858282495202271984454410046682805375157566e-03,
                    -1.696456818974824394274534636412116243080312601322325642741589e-03,
                    -4.416184856141520063365958900079406737636243682138363561877750e-05,
                     5.861270593183109933716735450272894035425792347806515678695765e-04,
                    -1.181233237969554740613021227756568966806892308457221016257961e-04,
                    -1.460079817762616838924301818082729036314539476811023255670666e-04,
                     6.559388639305634085303738560455061974369354538271316071502698e-05,
                     2.183241460466558363365044032984257709791187640963509380549307e-05,
                    -2.022888292612697682860859987200455702614855595412267510558659e-05,
                     1.341157750809114719319937553186023660581084151828593222893663e-08,
                     3.901100338597702610409014129024223853127911530009766793352492e-06,
                    -8.980253143938407724149926669980791166378388013293887718404796e-07,
                    -4.032507756879971624098983247358983425236092110387724315244646e-07,
                     2.166339653278574639176393978510246335478946697396400359281412e-07,
                    -5.057645419792500308492508924343248979317507866520688417567606e-10,
                    -2.255740388176086107368821674947175804005323153443170526520277e-08,
                     5.157776789671999638950774266313208715015419699643333784626363e-09,
                     4.748375824256231118094453549799175824526559994333227456737433e-10,
                    -4.024658644584379774251499574468195118601698713554294941756559e-10,
                     6.991801157638230974132696433509625934021677793453732225542951e-11,
                    -4.342782503803710247259037552886749457951053124203814185811297e-12
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 25-го порядка.
        /// </summary>
        public static WaveletPack D25
        {
            get
            {
                return Create(new double[]
                {
                     1.348029793470188994578489247159356055370460656508881471268611e-04,
                     2.256959591854779520121391049628056149270016860666661928130747e-03,
                     1.718674125404015533817186914954848902241194002444696221013131e-02,
                     7.803586287213267559750659320481403668422052199257139168386084e-02,
                     2.316935078860218199900621518057089104946216881512075361624214e-01,
                     4.596834151460945937896973864539659944010260858049947396093277e-01,
                     5.816368967460577833534892038757085635755639698734580573323031e-01,
                     3.678850748029466984371319740855532278670733841012809062966976e-01,
                    -9.717464096463814276130048169040892607068486428294030952842447e-02,
                    -3.364730796417461309562110148848845218930261030262170601615289e-01,
                    -8.758761458765466140226687673880006154266689569025041229545538e-02,
                     2.245378197451017129525176510409543155930843160711989062118482e-01,
                     1.181552867199598604563067876819931882639429216001523151773895e-01,
                    -1.505602137505796309518094206831433270850173484773520730186277e-01,
                    -9.850861528996022153725952822686729410420350758543226219234795e-02,
                     1.066338050184779528831274540522414711301747903916268438037723e-01,
                     6.675216449401860666895983072443984697329752470942906490126865e-02,
                    -7.708411105657419356208567671699032054872853174701595359329826e-02,
                    -3.717396286112250887598137324046870459877639250821705817221557e-02,
                     5.361790939877949960629041419546536897037332284703545849594129e-02,
                     1.554260592910229163981295854603203625062268043511894295387375e-02,
                    -3.404232046065334099320628584033729153497903561399447916116575e-02,
                    -3.079836794847036661636693963570288706232460663070983852354326e-03,
                     1.892280447662762841086581178691039363674755753459524525886478e-02,
                    -1.989425782202736494289461896386235348901617760816745484282494e-03,
                    -8.860702618046368399013064252456556969199612331833605310278698e-03,
                     2.726936258738495739871469244610042793734119359765762028996059e-03,
                     3.322707773973191780118197357194829286271392998979276105842863e-03,
                    -1.842484290203331280837780430014195744813667655929909114672154e-03,
                    -8.999774237462950491085382524008429604309720852269895692000702e-04,
                     8.772581936748274843488806190175921376284150686011179612908221e-04,
                     1.153212440466300456460181455345639872216326644527860903202733e-04,
                    -3.098800990984697989530544245356271119416614147098459162436317e-04,
                     3.543714523276059005284289830559259809540337561365927850248007e-05,
                     7.904640003965528255137496303166001735463107762646364003487560e-05,
                    -2.733048119960041746353244004225286857636045649642652816856524e-05,
                    -1.277195293199783804144903848434605690990373526086311486716394e-05,
                     8.990661393062588905369930197413951232059323587543226269327396e-06,
                     5.232827708153076417963912065899772684403904504491727061662335e-07,
                    -1.779201332653634562565948556039009149458987774189389221295909e-06,
                     3.212037518862519094895005816661093988294166712919881121802831e-07,
                     1.922806790142371601278104244711267420759978799176017569693322e-07,
                    -8.656941732278507163388031517930974947984281611717187862530250e-08,
                    -2.611598556111770864259843089151782206922842627174274274741722e-09,
                     9.279224480081372372250073354726511359667401736947170444723772e-09,
                    -1.880415755062155537197782595740975189878162661203102565611681e-09,
                    -2.228474910228168899314793352064795957306403503495743572518755e-10,
                     1.535901570162657197021927739530721955859277615795931442682785e-10,
                    -2.527625163465644811048864286169758128142169484216932624854015e-11,
                    1.509692082823910867903367712096001664979004526477422347957324e-12
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 26-го порядка.
        /// </summary>
        public static WaveletPack D26
        {
            get
            {
                return Create(new double[]
                {
                     9.493795750710592117802731381148054398461637804818126397577999e-05,
                     1.650520233532988247022384885622071050555268137055829216839523e-03,
                     1.309755429255850082057770240106799154079932963479202407364818e-02,
                     6.227474402514960484193581705107415937690538641013309745983962e-02,
                     1.950394387167700994245891508369324694703820522489789125908612e-01,
                     4.132929622783563686116108686666547082846741228042232731476147e-01,
                     5.736690430342222603195557147853022060758392664086633396520345e-01,
                     4.391583117891662321931477565794105633815363384084590559889493e-01,
                     1.774076780986685727823533562031556893226571319881417676492595e-03,
                    -3.263845936917800216385340830055349953447745005769416287177497e-01,
                    -1.748399612893925042664835683606584215248582345438816346170042e-01,
                     1.812918323111226960705459766025430918716233584167982942044424e-01,
                     1.827554095896723746537533832033286839689931924709760567945595e-01,
                    -1.043239002859270439148009137202747658420968144330108510179290e-01,
                    -1.479771932752544935782314546369458188243947772922980064071205e-01,
                     6.982318611329236513756591683950208955110603212379412334701145e-02,
                     1.064824052498086303236593797715344405836015002929319291715777e-01,
                    -5.344856168148319149493577269390074213960237013099439431132086e-02,
                    -6.865475960403591525454725258715351280947435823354011140858001e-02,
                     4.223218579637203541206570902753288247790857760067894456114927e-02,
                     3.853571597111186425832144567362328142994885395255438867968781e-02,
                    -3.137811036306775484244644776337594435094096964336402798072360e-02,
                    -1.776090356835818354094298625884058170354129044259951019182732e-02,
                     2.073492017996382475887790073068984224515077665517103399898854e-02,
                     5.829580555318887971939315747596613038479561943085291072787359e-03,
                    -1.178549790619302893728624468402138072504226527540325463847390e-02,
                    -5.287383992626814439198630765217969804966319971038003993984480e-04,
                     5.601947239423804853206514239940474788977188460452053462770324e-03,
                    -9.390582504738289646165698675070641765810790863514339205205998e-04,
                    -2.145530281567620980305401403432221668847980295600748913748902e-03,
                     8.383488056543616046381924054554052104937784379435436426690560e-04,
                     6.161382204574344193703789012696411561214682388271673214197731e-04,
                    -4.319557074261807466712901913481943478521991611607433971794602e-04,
                    -1.060574748283803889966150803551837402553866816191659959347053e-04,
                     1.574795238607493590547765666590811258087715699737771458390360e-04,
                    -5.277795493037868976293566636015627609248847457646525246271036e-06,
                    -4.109673996391477816326502438997466532822639385119090230965252e-05,
                     1.074221540872195031273584409245060623104931330938273936484593e-05,
                     7.000078682964986734859102495210684809643657474253921074934684e-06,
                    -3.887400161856795187587790410706550576033603097954065074023128e-06,
                    -4.650463220640262639231145944536092973446596027469833860001618e-07,
                     7.939210633709952088373459255067360793370284788682979065122810e-07,
                    -1.079004237578671411922961583845716126060658213943840375162654e-07,
                    -8.904466370168590769052983362721567202750591914741016835071257e-08,
                     3.407795621290730008673832107214820587991557116806912418558069e-08,
                     2.169328259850323106986222296525930099935873861026310788086221e-09,
                    -3.776010478532324328184043667556576385639846460337894963138621e-09,
                     6.780047245828636668305808192607091517605349478677442468580825e-10,
                     1.002303191046526913509281844136258004034177309673269533418644e-10,
                    -5.840408185341171468465492447799819262905317576847426970757700e-11,
                     9.130510016371796243923232926650252570239054815939483900056681e-12,
                    -5.251871224244435037810503452564279828539007071678724285717464e-13
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 27-го порядка.
        /// </summary>
        public static WaveletPack D27
        {
            get
            {
                return Create(new double[]
                {
                     6.687131385431931734918880680779563307675740731544063787599480e-05,
                     1.205531231673213234251999812212394463872002561229330125152073e-03,
                     9.952588780876619771874091297340545740163119816300838847749336e-03,
                     4.945259998290488004302995584228917712171023349013386944893643e-02,
                     1.629220275023933206396286389387812803673796872000118325233533e-01,
                     3.671102141253898226423388094379126394383458407087000700420400e-01,
                     5.538498609904800487605460395549044755068663194750017660900436e-01,
                     4.934061226779989979265447084358038959373468583404767251300717e-01,
                     1.028408550618229112710739475157388764479351682549490307668477e-01,
                    -2.897168033145948463175311101489473923261698802610323264603418e-01,
                    -2.482645819032605667810198368127693701263349361209208170092197e-01,
                     1.148230195177853576326445213787661879970642975306605349249036e-01,
                     2.272732884141708265275037216925482827043581894357907763081103e-01,
                    -3.878641863180231062443346843661817078060143110529946543683356e-02,
                    -1.780317409590085821070366277249759321269342801053489323888575e-01,
                     1.579939746024048431173907799261019471878724997312653292884660e-02,
                     1.311979717171553289711406975836688896451835867594492827800969e-01,
                    -1.406275155580876537026622167053147161846397735962817855782362e-02,
                    -9.102290652956591798241345515773322449830692586525337562864481e-02,
                     1.731101826549371089085675445961947677452358872325373949295769e-02,
                     5.796940573471798814748840657698008349462526768238833307489106e-02,
                    -1.851249356199807710545837861298826718763077900221574749342712e-02,
                    -3.273906663102087145481936428049519742538150452785563039743756e-02,
                     1.614696692239566682272152627542980896527822528487665111124260e-02,
                     1.566559564892457873003263983940819950829497022298967052103291e-02,
                    -1.157718645897628140054089958116866381056430680879332334217267e-02,
                    -5.862096345462925972966025215266179082657169806555503857975278e-03,
                     6.856635609684880675273184141746359000591385833807880272568038e-03,
                     1.342626877303679609082208800217479591902967766971379107017011e-03,
                    -3.332854469520006162763300141047111065412307706449049389557931e-03,
                     1.457529625931728587128588244152604734177322144376309490881599e-04,
                     1.301177450244135139135787970279897042994109161268159963884641e-03,
                    -3.418351226915427611946547437228006377896519777431057005796358e-04,
                    -3.879018574101327604369144470124819695479087900682219330965466e-04,
                     2.019719879690326857104208791272390315160018069955787875123234e-04,
                     7.660058387068576876674274961751262847965101108848090019821555e-05,
                    -7.711145517797584208411720507329584053382646435270054267102827e-05,
                    -3.517483614907445391752737841583832374184046409747387149129674e-06,
                     2.063442647736885318487206413360228908558806028468062177953960e-05,
                    -3.901164070638425528170558032557368703418425915665413541985623e-06,
                    -3.657500908187104997045760131046655906827644494899206692043298e-06,
                     1.634369624725637835424610743915128591988676092276368687669255e-06,
                     3.050880686251999094242671997731089918322345713516567387655763e-07,
                    -3.472468147394389269364673179891460601330730511237974736379548e-07,
                     3.286558968055159530983261866450459360074591641809187825408848e-08,
                     4.026255052866908637178682747490340533992340623231336911661711e-08,
                    -1.321332273990056558848617809101876846857728483295631388083263e-08,
                    -1.309465606856955151282041809232358209226373823424148862843577e-09,
                     1.521614984778521740775073159445241799352681846880808663329946e-09,
                    -2.415526928011130660506395791946234018673860470542996426005750e-10,
                    -4.374986224293654395069947682013996351823060759948583134078918e-11,
                     2.213662088067662485181472969374945928903854605356443772873438e-11,
                    -3.295790122476585807069953975043096139541415768606924980926275e-12,
                    1.828188352882424933624530026056448539377272017834175009418822e-13
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 28-го порядка.
        /// </summary>
        public static WaveletPack D28
        {
            get
            {
                return Create(new double[]
                {
                     4.710807775014051101066545468288837625869263629358873937759173e-05,
                     8.794985159843870273564636742144073059158975665525081816488582e-04,
                     7.542650377646859177160195786201116927568410621050693986450538e-03,
                     3.909260811540534426092083794403768111329778710541126982205076e-02,
                     1.351379142536410450770749411679708279921694061092200363031937e-01,
                     3.225633612855224257318486139030596702170126503618082416187649e-01,
                     5.249982316303355562348293243640252929543774162151269406404636e-01,
                     5.305162934414858075256978195354516449402692654391295761050628e-01,
                     2.001761440459844380384404537971725815970574972480152145882083e-01,
                    -2.304989540475825257279397658067038304888129374484095837624889e-01,
                    -3.013278095326417816909366061441334075444383937588485826752087e-01,
                     3.285787916338710468450547883547348694255260871071954509422161e-02,
                     2.458081513737595535752949960866466132239832334168533456626848e-01,
                     3.690688531571127205290633425993077868843846977265847006108551e-02,
                    -1.828773307329849166920408764650763092868965221608724574218473e-01,
                    -4.683823374455167616514752420549419665215987106243491879971921e-02,
                     1.346275679102260877490923315484152662987698625205479167761416e-01,
                     3.447863127509970524678534595639646616244376966117385829345554e-02,
                    -9.768535580565244174963692133038973587005628990493154911133358e-02,
                    -1.734192283130589908795581592406238282930530566316914040035812e-02,
                     6.774789550190933956165341752699717255041141690153626336867769e-02,
                     3.448018955540951137600471926079622335842207388713342609755316e-03,
                    -4.333336861608628393863254980828284403766309203453808666888800e-02,
                     4.431732910062988320487418656322338284504389482966303454010563e-03,
                     2.468806001015186586264188361362046240243934625858343309818244e-02,
                    -6.815549764552309639259447104811254179605050667281644254737890e-03,
                    -1.206359196821849005842466619530619474644989878503490321948471e-02,
                     5.838816627748944864497370576838809711476027837762897602935327e-03,
                     4.784863112454241718009916669120329848973107781600157214960003e-03,
                    -3.725461247074254799171427871442937099025589672466088044410521e-03,
                    -1.360373845639692436577650137133777929659265166644839235882291e-03,
                     1.875998668202795626152766912508562385106168761893900192731562e-03,
                     1.415672393140464257573780581396205840941849282748250523509874e-04,
                    -7.486749559114629991320679819683227355746847370960399216568306e-04,
                     1.154656063658921251969297916771881248142872975490882572741198e-04,
                     2.295790982233456202366621544054366855729175050420515776344878e-04,
                    -8.903901490044488099517361247378396756893227855233897357882978e-05,
                    -4.907713416190250858324783990436748073854807494400738311968278e-05,
                     3.641401211050802781223450761733180188911730291497201507086247e-05,
                     4.638664981394294654002871426476885751050837817671843706915388e-06,
                    -1.004326041333422601781848560432120920634648692782357855473103e-05,
                     1.247900317574834146052381692752796047052443265982232422642017e-06,
                     1.840363734517769191684379309039277810350620305330900536404818e-06,
                    -6.670215479954892588747450458085225880096882699397256774967304e-07,
                    -1.757461173209842779903676264971918635870906983281392939812547e-07,
                     1.490660013535362170989340065033061951960933954388633507264360e-07,
                    -8.262387315626556965966429243600984899650039704831080988658278e-09,
                    -1.784138690875710077191713941441263246560738410213624546116655e-08,
                     5.044047056383436444631252840057862002264087720676808580373667e-09,
                     6.944540328946226952976704718677697525410051405055662575530111e-10,
                    -6.077041247229010224760245305596307803830053533836849384680534e-10,
                     8.492220011056382105461206077240377024404404638947591299761197e-11,
                     1.867367263783390418963879146175452376940453585791428841004699e-11,
                    -8.365490471258800799349289794397908900767054085216008197372193e-12,
                     1.188850533405901520842321749021089497203940688882364518455403e-12,
                    -6.367772354714857335632692092267254266368934590973693820942617e-14
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 29-го порядка.
        /// </summary>
        public static WaveletPack D29
        {
            get
            {
                return Create(new double[]
                {
                     3.318966279841524761813546359818075441349169975922439988843475e-05,
                     6.409516803044434540833706729120596322083061716935004987374676e-04,
                     5.702126517773375434760843998623507494914551464968126455168657e-03,
                     3.077358022140837676716707336516751814713312018344719150923618e-02,
                     1.113701169517405304762186166370327770191325772342190715118617e-01,
                     2.806534559709829376968881262770480606500920092398534229615289e-01,
                     4.897588047621993143592705932993573539235839610055331620240518e-01,
                     5.513744327583751951223746071670135992466984391233429663886536e-01,
                     2.891052383358291634605691113586264061513180158354460952469246e-01,
                    -1.540287344599000542466293779503370141731339982919280951230240e-01,
                    -3.300409489175880520295083779487012611959310539629627124613719e-01,
                    -5.570680007294085781514541931715795784309410235726214400350351e-02,
                     2.361052361530259415983110734054626770649468357328362426830433e-01,
                     1.124191748731883764769740670535880543076817816861518667898467e-01,
                    -1.608779885941877360771615465531852333085159940159968393590303e-01,
                    -1.078459499387214201077881957354707913786241153934264316589273e-01,
                     1.144722958938182579734135930060053286267822797640393386903440e-01,
                     8.322074716244975790297348835032537357891920536002627784941129e-02,
                    -8.512549261563550232832311331420804581881235448862834507281486e-02,
                    -5.502748952532572320924541450626650067707344725344841099873446e-02,
                     6.347916458421186633577789314698972361081611994794140119302163e-02,
                     3.053154327270413646637328212093941030592133225231728964047047e-02,
                    -4.518798127778834515979704475304405691390090327474972089790857e-02,
                    -1.291714255426679462966473962555410660387671182428076570686472e-02,
                     2.947043187174764111028122319949903667638786379520519899154373e-02,
                     2.648327307678167915542397563479749119673768286990136051577167e-03,
                    -1.704122457360668969234196743407615179099529206118693044741086e-02,
                     1.737880332720511164430027824345354801611373419264590068097416e-03,
                     8.469725493560752287772961661104710791306496373354237126998903e-03,
                    -2.550807127789472659145072247724735637183590942511858255354005e-03,
                    -3.473798989681100630649790255076233970957721666820195620598374e-03,
                     1.877120925723650133179338154344873477230567340668548016358682e-03,
                     1.087053942226062966738944397844498417945523630053411148182206e-03,
                    -1.000778327085680541055696707760062870925897014530348262794137e-03,
                    -2.000711363076779808296301110796026470163110202848894744316755e-04,
                     4.111283454742767033424740543004041500054889660665367490129376e-04,
                    -2.292018041214499897382298271438084577065170236103859181134525e-05,
                    -1.293044840080720609161466939678226852440475312744714379499074e-04,
                     3.645026068562774967665464216602750761690984830805534178557146e-05,
                     2.913344750169041218495787251929571015775436967652945386217480e-05,
                    -1.657328395306616289863396387854880512976861409870690029695161e-05,
                    -3.593644804025187638066915189731950450034629392522542962477168e-06,
                     4.750609246452552850197117564759363194953518317428400241629683e-06,
                    -3.029054592052818286474228294307141792053791695855058563299597e-07,
                    -8.975701750636280734511651941681818767895052287332471537510510e-07,
                     2.633898386997696553900967704111473475368019612368922599394214e-07,
                     9.387197411095863026484410601284876812292554863800653292318725e-08,
                    -6.286156922010786166768503252870590953166867739448102804392389e-08,
                     1.076591906619196137385201975028785139607670319821266803566785e-09,
                     7.768978854770062238895964639391324551611701293594055935346266e-09,
                    -1.893995386171984147774611076618946011337498790609031626697228e-09,
                    -3.426800863263089001811012278889864200550342566386405676893537e-10,
                     2.407099453509342962399811991929330725186626582891090462239366e-10,
                    -2.940589250764532582888473974638273664244682541297835986306504e-11,
                    -7.832509733627817032356556582819494794884131433810848844709881e-12,
                     3.152762413370310423797539876893861621418382024668704492620948e-12,
                    -4.285654870068344101898185073376307686875386259541180967347399e-13,
                    2.219191311588302960934661700068023727737812918006011019184982e-14
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 30-го порядка.
        /// </summary>
        public static WaveletPack D30
        {
            get
            {
                return Create(new double[]
                {
                    2.338616172731421471474407279894891960011661146356580425400538e-05,
                     4.666379504285509336662000111055365140848987563882199035322085e-04,
                     4.300797165048069510045016757402827408493482974782286966500398e-03,
                     2.413083267158837895194919987958311943976725005113561262334092e-02,
                     9.123830406701570679321575555085899708564500191080751595642650e-02,
                     2.420206709402140994467599658342919512318194032687898436229538e-01,
                     4.504878218533178366981351802898336415314944375740699506554771e-01,
                     5.575722329128364304078082520999850413492571645754785374629734e-01,
                     3.662426833716279793144871151369089533016299234992584741629624e-01,
                    -6.618367077593731501909741041813726474911212544474895441395148e-02,
                    -3.329669750208556069196849320598850505877494561268613506392514e-01,
                    -1.419685133300829310219026267403758254954270602825020111483505e-01,
                     1.994621215806643032428990062111230223523226088131364328774921e-01,
                     1.778298732448367361280250921330425046260289700971176750362566e-01,
                    -1.145582194327077814891518778613672243404957549114393749173137e-01,
                    -1.572368179599938126878197378886501553251711910617673398124611e-01,
                     7.277865897036442699893544326605244235248713804556715604416632e-02,
                     1.227477460450093778691578797698150091624353365248212907325446e-01,
                    -5.380646545825707676022015051837304300338645984615639237930800e-02,
                    -8.765869003638366048026572053699028353846982304851342479893827e-02,
                     4.380166467141773250305407710250135373016604593736480428415303e-02,
                     5.671236574473569492590636983030617493807140224924978946302257e-02,
                    -3.567339749675960965780819743176056734137251336781389369397564e-02,
                    -3.226375891935220815954913483392725682165778426411705216010280e-02,
                     2.707861959529418272206848318420006522973840949600186710327776e-02,
                     1.528796076985739546052896626042375110302102640936712142026221e-02,
                    -1.839974386811734118728169880549148389603890445324127330811811e-02,
                    -5.296859666131086629169938675330494864053932988161015674773617e-03,
                     1.091563165830488927536881480211929049886878831313700460017968e-02,
                     6.196717564977244383592534999284255315694546230739551683085460e-04,
                    -5.530730148192003288871383856487027893918513053091795443517653e-03,
                     8.433845866620933982126003584365932145598126087481400294999080e-04,
                     2.324520094060099304385756339638431339131122661576649123053845e-03,
                    -8.609276968110423879660725173525347077801305237644122054954659e-04,
                    -7.678782504380918697963922441514742758516706160788123977340073e-04,
                     5.050948239033467796256544554086554367969638627715114003635557e-04,
                     1.724825842351709725545759714374272164367933578194910678479473e-04,
                    -2.161718301169633804271038862087964094429005266172702380483361e-04,
                    -8.548305467584070994787824796256108217987765582429940610377190e-06,
                     6.982008370808327851082027193100914402221658444151889697045071e-05,
                    -1.339716863293971629296314599448901465078920406443516550195793e-05,
                    -1.636152478725426488654528710478856195004608401773950511915162e-05,
                     7.252145535890469015723401169934327900622894130695550273452916e-06,
                     2.327549098493686509557358103785598216688723737824121617676858e-06,
                    -2.187267676996166416699555236143059249832615777542412142603694e-06,
                     1.099474338526203304286307383463498542376432972308342428764576e-08,
                     4.261662326011572446469849114416378817419458434583398455985144e-07,
                    -1.000414682354500898864979332965559934104686157639553850670490e-07,
                    -4.764379965139453357729154748688006975561934425368712852985388e-08,
                     2.605442754977625431940885841950955928085338672381046225838880e-08,
                     5.553397861397053982967618072672572206490972606026556946910028e-10,
                    -3.331105680467578245901976412732595596538702049437802824373020e-09,
                     6.984862691832182584221096665570313611280449991512869846064780e-10,
                     1.613622978270904360610418704685783656905979134344922647926295e-10,
                    -9.461387997276802120884525814092001871993910062127702293573920e-11,
                     1.000105131393171192746337860330428369495110180346654025287492e-11,
                     3.239428638532286114355931428908079297696045600279108835760520e-12,
                    -1.185237592101582328254231496310584611948560976394420324137742e-12,
                     1.543997570847620046003616417646988780670333040868954794039905e-13,
                    -7.737942630954405708679963277418806436871098329050829841696327e-15
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 31-го порядка.
        /// </summary>
        public static WaveletPack D31
        {
            get
            {
                return Create(new double[]
                {
                    1.648013386456140748122177817418358316441195236228590958603489e-05,
                     3.394122037769956699157160165352942212213928231154233571163033e-04,
                     3.236884068627721221829662672296912258338131668810067169630813e-03,
                     1.885369161298591269159568944275763468999829139547989648553486e-02,
                     7.433609301164788697908776495388047669378919816041031344650271e-02,
                     2.070128744852353286198055444111916450619762837756134323019573e-01,
                     4.091922000374278563928213235836188963704298775635493549519369e-01,
                     5.511398409142754983590484577074663132074992263886810324421617e-01,
                     4.294688082061372955430413148799008354573408538414331312236645e-01,
                     2.716921249736946422305354732634261873401679092095992827198308e-02,
                    -3.109551183195075186926560285811004715398678229333522634202008e-01,
                    -2.179784855235633521693544507220105631639547435903112747133934e-01,
                     1.401782887652732681656253206993073895422881511380152633441096e-01,
                     2.249667114737370933697297905066886078307490136415302624018330e-01,
                    -4.992634916046823977000579399730138693074543903234092797936484e-02,
                    -1.869623608957154494374577196258383009208655076187653847079167e-01,
                     1.543698842948893409652995335281236231845293548571166883219023e-02,
                     1.450895009319931981518942907854879059128872873116921504156674e-01,
                    -8.139832273469236863527708715566588550006680549152344840146851e-03,
                    -1.076127733234956326668605511648013952380301953590447106075614e-01,
                     1.094129745236496925725237900637802669504835743555466811796369e-02,
                     7.535361174328140695528289751109133941376701984419452638686226e-02,
                    -1.488002661810482202699555987503429289100801979910046913257306e-02,
                    -4.861907546485433003537603385831190109391263542044516048871113e-02,
                     1.615417156598591113619453864586701665635869166193865651960591e-02,
                     2.804761936675616906861927211659154977049392281479113764697785e-02,
                    -1.427627527776351943309800140756746087215016194775579070599004e-02,
                    -1.390055293926652880755898888934447671732373519028670201124816e-02,
                     1.051763948737184089128633441244991643331033825102031908858652e-02,
                     5.516163573310992566561289762241160214476622662764637181816550e-03,
                    -6.520852375874612553325469682628530079210293774541131381751695e-03,
                    -1.428264223218909891400516038687842292177211292295049238921068e-03,
                     3.393066776715931928419358796960612411097347419792355896915546e-03,
                    -6.397901106014600492881202314307290077992972755016494062875201e-05,
                    -1.459041741985160943114515221598080223845239255190055621901681e-03,
                     3.431398296904734438118401084929505912208229684629857530009147e-04,
                     4.998816175637222614896912406679513231966722440032799024979502e-04,
                    -2.396583469402949615285646688069476140260781708006174912535660e-04,
                    -1.243411617250228669409179807383399199879641177993453588807726e-04,
                     1.089584350416766882738651833752634206358441308880869184416670e-04,
                     1.501335727444532997071651937630983442758297688087711521441229e-05,
                    -3.631255157860086164261313773172162991107348698083164489165837e-05,
                     4.034520235184278839752741499546098778993926344831736074409765e-06,
                     8.795301342692987765440618030678349427367022581211855857458220e-06,
                    -3.035142365891509630069007852947057220760887215249503512783023e-06,
                    -1.369060230942940782050489751987123955074404782177163471279285e-06,
                     9.810015422044371573950976088058064384946146188110905321673802e-07,
                     5.327250656974915426977440959783080593776012130063170688309127e-08,
                    -1.975925129170206248152121156696590501303803187231928513867046e-07,
                     3.616826517331004805247567218405798591329788122337274956172315e-08,
                     2.328309713821409644308538888589329921141948539678106680777082e-08,
                    -1.061529602150252306500404266150823962402673780484965538270541e-08,
                    -6.474311687959861398702581539341954438747926255671605657095807e-10,
                     1.408568151025177427076547804944585301332087108125727813194374e-09,
                    -2.524043954153353306183643702933218308617979467184848456565837e-10,
                    -7.348930032486263904766913919653624379586487437915175106407348e-11,
                     3.692108808871129411604189196259677640440919369478263728899602e-11,
                    -3.327008967125979929910636246337150851642079794871116041187279e-12,
                    -1.324334917243963163878274345609465717294426628053460151843705e-12,
                     4.445467096291932163298411852093011459626037560439178917611592e-13,
                    -5.559442050579014337641375730083534521513818164827556763756543e-14,
                    2.699382879762665647295493928801387173921314576598505507855504e-15
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 32-го порядка.
        /// </summary>
        public static WaveletPack D32
        {
            get
            {
                return Create(new double[]
                {
                    1.161463302135014885567464100760659332951431420121048996305591e-05,
                     2.466566906380903352739104211274667134470169443886449124673996e-04,
                     2.431261919572266100780423071905958127811969678055971488060574e-03,
                     1.468104638141913563547809006402194831107662001343421893488086e-02,
                     6.025749912033537081745451975527967031851677384078997261920024e-02,
                     1.757507836394388988189299915753348505208376399651864661397588e-01,
                     3.675096285973496361995340339143234125206079560406868595968025e-01,
                     5.343179193409538322901117858552186425529774700290587495921679e-01,
                     4.778091637339484033555130814414794130354053753675509287934741e-01,
                     1.206305382656178269538098710665261299391507308342013788891222e-01,
                    -2.666981814766755535489784087869865024226542605534080371507405e-01,
                    -2.774215815584272153338153320303401666681294506143291967655666e-01,
                     6.471335480551623831000090095167664918448659157720155321560811e-02,
                     2.483106423568801736064852157222867588791898170114101300999760e-01,
                     2.466244483969740441701479334808723214802614938081258920635302e-02,
                    -1.921023447085468984341365278247990525863123891147783426068990e-01,
                    -4.899511718467173853355943225576377418394280156945986899417475e-02,
                     1.452320794752866460838830744051944832326998342053148426312341e-01,
                     4.440490819993974022640619534046603571086531544468421519143629e-02,
                    -1.094561131160893831027722774343269232755171130623890041619420e-01,
                    -2.962787250844770491204452379051215505049068645551070779367843e-02,
                     8.087414063848395744090831590426327690818854671836423275412813e-02,
                     1.410615151610660772869738802931740150275269382463799031013905e-02,
                    -5.692631406247843550478416271158537960555270097953330567652364e-02,
                    -2.380264464932573834443178362086503847328134994591954135879789e-03,
                     3.705145792354468010437633458013030898015496905609424004450953e-02,
                    -4.145907660827218781460700428862611061267328108653649653634276e-03,
                    -2.166282283639119347634778516947485598599029367518033869601702e-02,
                     6.167527310685675112579059689520105004744367282412921739811164e-03,
                     1.101740071540688116532806119564345712473051769079712407908648e-02,
                    -5.411568257275791208581502410752383050600045942275647685361370e-03,
                    -4.649216751184411528658094984504900172989190128905887602541396e-03,
                     3.627224640687864960122122984391704782343548385375321260251988e-03,
                     1.468955100468467772528811782840480639166582822577191079260543e-03,
                    -1.964740555821778254183647540656746450092725858126595984907304e-03,
                    -2.211678729579097916278097586914956834196749138610403102772710e-04,
                     8.673058518450555343925662389563539890596549655683386287799624e-04,
                    -1.024537310607396186949656796812972062290796122915930356634122e-04,
                    -3.059654423826911750479261161552574500739091332121504634422577e-04,
                     1.053915461739828114700905192091104141076083602686374410146603e-04,
                     8.103678329134838389828091896334156224227821362491626044950428e-05,
                    -5.259809282684322782648914338377962890245975842272425408122506e-05,
                    -1.294045779405512723950480259110995722517019870286295908085366e-05,
                     1.824268401980691220603850117995712615809177092802967489081228e-05,
                    -6.361781532260254953363913076575914206506177493714496098327288e-07,
                    -4.558309576264423135123964145585288808181431652781253437738445e-06,
                     1.202889036321620990296134494079846952404216422923750605507047e-06,
                     7.560047625595947819392627283726711361273296630256477108501994e-07,
                    -4.285970693151457255418342315045357407199066350632593899896712e-07,
                    -5.003361868748230293692887222336390314786090450819216035110269e-08,
                     8.965966311957728376981484572655177545054433542721057470726361e-08,
                    -1.219924359483373093110396748985081720383992859961285213840740e-08,
                    -1.104383021722648979552131128575075255513372249283096583736746e-08,
                     4.250422311980592983740943309197245384991941251563471671065543e-09,
                     4.384387799940474369553236949848427579687147486892033587998023e-10,
                    -5.881091462634605628881794361152305108432139465417759716875076e-10,
                     8.904723796221605490455387579189371137903330749397374037644960e-11,
                     3.263270741332907875981844980104948375955551273115386408552080e-11,
                    -1.430918765169202320188022211739750594608742928641485026836608e-11,
                     1.075610653501062115165734990153347111902874668945095034791947e-12,
                     5.361482229611801638107331379599434078296259332654994508124989e-13,
                    -1.663800489433402369889818192962259823988673359967722467427927e-13,
                     2.000715303810524954375796020597627467104635766752154321244151e-14,
                    -9.421019139535078421314655362291088223782497046057523323473331e-16
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 33-го порядка.
        /// </summary>
        public static WaveletPack D33
        {
            get
            {
                return Create(new double[]
                {
                    8.186358314175091939858945975190102731733968885547217619434602e-06,
                     1.791016153702791479424389068736094134247294413108336017758506e-04,
                     1.822709435164084208084617771787691709255513374281497713580568e-03,
                     1.139594337458160925830840619716397130445853638888472948832932e-02,
                     4.861466653171619508385707681587366397164931431125053574327899e-02,
                     1.481863131800528081784673514426737436792606299953305691300616e-01,
                     3.267181301177075783930752787756046348844272437670999719562429e-01,
                     5.093761725149396552227892926384090200953139820961482931291482e-01,
                     5.112547705832674655425831875568453973369927971748064975152374e-01,
                     2.095823507130554216526494469993023406452629154801126958766008e-01,
                    -2.042026223985421049629055102642279430174095014493415546881477e-01,
                    -3.159974107665602561905181464284910961862968513875028980451424e-01,
                    -1.927833943695275915600583425408664108893845271616240406358226e-02,
                     2.454206121192791114179964351253140999836791489738418857473689e-01,
                     9.985155868033815698139640215477639365289384281516885362929979e-02,
                    -1.714280990518593279308738113273443832545615219650436927029674e-01,
                    -1.108441331167107910806084983056783194189909198734302929909672e-01,
                     1.219678564037346149389134584371009777591763921148126952722200e-01,
                     9.478808805061595889263191779090571160237408179346345390888721e-02,
                    -9.114696835133148913093153757138373418923462847746880902676089e-02,
                    -7.030248505405615921453280814171665167171986608963193275084895e-02,
                     7.019114394099653254998935842432841393915841096633514680190145e-02,
                     4.573456189389667743139040427641638967843459421665709740086516e-02,
                    -5.347125133582228919431110824663168583260050383336359554980188e-02,
                    -2.524858297747649929258392207837724793937727346177294684700378e-02,
                     3.868706076024496481748675031852528047303323816250150793091832e-02,
                     1.070326582001954942654534968137727769698168853186071888736311e-02,
                    -2.572876175473297336123211392278301875687760837710204579628265e-02,
                    -2.167758617353607324783298657172830203896433848418061622436727e-03,
                     1.531695411585766548347442266431874060229304787191589430967538e-02,
                    -1.594288782414604768637856446111392724059836934455189837500244e-03,
                    -7.953540387057939240459305406538116220678495240302592677582773e-03,
                     2.389062408165908575935815973439728988151836094753689966108405e-03,
                     3.480800953405711999411461002429227385937942254778524257436278e-03,
                    -1.860718214455795912074482150710567824317228203897000129729967e-03,
                    -1.204309257604658876916644980097327372892008586047095719636829e-03,
                     1.074380696351291355073899234941719080473877020595209197706651e-03,
                     2.727305847336937211749282358350196461733595290569540045817329e-04,
                    -4.908329007590351474487792254066540683724948757382104652497458e-04,
                     4.393166251766185755059005296958129844094063524324718175254673e-06,
                     1.780431898251245351831728023200069586928513661382622116969992e-04,
                    -4.160438516273709306234368807933932360567787692918883118883736e-05,
                    -4.929564423417301834310231482621574127409950921583062559483686e-05,
                     2.423335398816890365621188379922041046073808819182024026589770e-05,
                     9.070805757828453800203677464921508178468256685438211818575040e-06,
                    -8.866121366757736169176034432364298134186929098274651022820760e-06,
                    -3.607516102879771631230351118595069330196155459105589342866625e-07,
                     2.288371276141527305481395545993763010565968667577768164201792e-06,
                    -4.426923407952870147984002129341809185622768353983550670755106e-07,
                    -3.985791291985944076942626511739220753169387460984290019185514e-07,
                     1.822443332571053437467128998002798233969112236553215291639303e-07,
                     3.377972703730854377516206663481869099376154259897212784144779e-08,
                    -3.987838198518880722819502850814936369197384392561970319349663e-08,
                     3.672863576838181340505563759379169099717712645283448779390320e-09,
                     5.111211857347453839549366593998758891130921028374576213256027e-09,
                    -1.671392677251932495173219614104411841891545601521784559793012e-09,
                    -2.496402105246193648073519269370197331176405371538404298745013e-10,
                     2.426833102305682309891302883361232297664099485514601790344279e-10,
                    -3.049574453945863430361296931455141500128170151643206937547928e-11,
                    -1.420236859889936792437077844940412749343225644487770840543290e-11,
                     5.509414720765524548752673631197714447818740985929081064907524e-12,
                    -3.343481218953278765982532722689984725170758193566174566492199e-13,
                    -2.152488386833302618520603545685994753329478275805993737095214e-13,
                     6.214740247174398315576214699577230693021307854673557214652751e-14,
                    -7.196510545363322414033654470779070592316600780697558361083151e-15,
                    3.289373678416306368625564108782095644036415401902518812978798e-16
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 34-го порядка.
        /// </summary>
        public static WaveletPack D34
        {
            get
            {
                return Create(new double[]
                {
                    5.770510632730285627466067796809329117324708919047900817738025e-06,
                     1.299476200679530037833484815390569400369432658207722720405084e-04,
                     1.364061390059049998200014449396877439591680435610837369411339e-03,
                     8.819889403884978803182764563095879335330977939541630862804757e-03,
                     3.904884135178594138905026219591569204043816577941517019631916e-02,
                     1.241524821113768081954449898210969172708199672428635378051285e-01,
                     2.877650592337145629334256618087718872558560120999651277991839e-01,
                     4.784787462793710621468610706120519466268010329031345843336104e-01,
                     5.305550996564631773133260223990794445605699030503652382795600e-01,
                     2.903663295072749510455945186199530115755664977934564128822650e-01,
                    -1.282468421744371672912377747048558427612774932943748628650824e-01,
                    -3.315253015083869417715548463087537345035828886426345397256876e-01,
                    -1.038919155156404718287260506925867970596448618647006698388596e-01,
                     2.169072201874275950610018667099322465619408030256534197819784e-01,
                     1.666017504122074437311574334509261366682993700573488534577890e-01,
                    -1.273373582238011562843862636988693890108793629966541695807247e-01,
                    -1.609249271778668063014799490429649196614628857267382976958607e-01,
                     7.799184693794810738265349531832015087096882277333968473726399e-02,
                     1.341259602711361284802399913977387999358280900708582462625539e-01,
                    -5.448296806413904636632671383140642554265865948686157271017286e-02,
                    -1.029475969928140852342073823689090498245496056845473569066667e-01,
                     4.357609464963129726428486610925800727137724136370669421246609e-02,
                     7.318523543679560555546221335452045680757998947493883124934567e-02,
                    -3.701283841786244960356402125554190040750079009127461655784927e-02,
                    -4.743855964527776247220681410983851377889756018716427358008296e-02,
                     3.073974657395934459931226513844134346305562928466993208164603e-02,
                     2.722835075635419610095839895805858855202745897718117731496534e-02,
                    -2.367173792282636485046786438094940427456079528043555566867110e-02,
                    -1.314398001665716086105827506126287041342680578404007359439612e-02,
                     1.640937419986519252112261495537409592363156309874473310057471e-02,
                     4.713649260999809905918876125437488856235874027077755004539205e-03,
                    -1.004550670836151917439146861146431000364858401181337134891421e-02,
                    -6.194748845153872839014356621835501857322345445234809347431098e-04,
                     5.334950768759936032170270195983921511565539100791906952901398e-03,
                    -7.692127975067836975989490900561029844887285335804349474993607e-04,
                    -2.399453943537055863933124827688081952701780599883067560501870e-03,
                     8.589959874363661955444898475746536583497522107459291718900058e-04,
                     8.751999064078688732610570055224339733760304773327228476255647e-04,
                    -5.527355762144197975516415296735124460550632283763688359649888e-04,
                    -2.326732140233531635428863212833942245597361085708567528230733e-04,
                     2.650772397558057819755811309071002543822145660933016957735937e-04,
                     2.660050018453441903046828468025589086403126180798464347801678e-05,
                    -9.914697770780134603580350758869378471802751837608461971022567e-05,
                     1.353117227249649581251887376414486225127346352042209141315562e-05,
                     2.844951419697807376503080001943765930601242225183893658540032e-05,
                    -1.057657494257950623848316304755218120233253479317574337409622e-05,
                    -5.710826510998303938275050074333400305512451419983646591762318e-06,
                     4.169871758547028398316761659984928804362023643629741358799744e-06,
                     4.979718101421307748081857636471761057429219265531618602960147e-07,
                    -1.116306534817008428597995070751765080383261658112656948526954e-06,
                     1.448195708333185127061180618150009526758658641231104901703561e-07,
                     2.025990666667859216690536885693725545344933235432307649205497e-07,
                    -7.526701740412589411177481797841044281662555785969415398369019e-08,
                    -1.990346501531736915866180448337614967570744211158241514589121e-08,
                     1.740423332936068076497051274445147160190783847854409836489662e-08,
                    -8.665744261368722215864741166245385888818567571145958531936939e-10,
                    -2.316501946995482751582294240136010067415084499025753117941001e-09,
                     6.446378210323402313101214894500231181606520211579581132442548e-10,
                     1.300410318609415248880403259300467720631189120978928377152233e-10,
                    -9.904774537632409015479530333979124540183199174591377762845227e-11,
                     1.004208735461769864836516428998306778031143650101842361622330e-11,
                     6.080125354000167254059025929915591291115751734288584563131636e-12,
                    -2.107879108915301546285370395443778864676275235126044599683271e-12,
                     9.799451158211597727901178520526388692140586041163624252991805e-14,
                     8.579194051799733179793112298652600511486581216528683482143106e-14,
                    -2.317083703906408481078257081903089523234020423092175261925515e-14,
                     2.587338381935699555813538163144986688834142571207152879144731e-15,
                    -1.148944754480590128244815794312606245147888158018823490936280e-16
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 35-го порядка.
        /// </summary>
        public static WaveletPack D35
        {
            get
            {
                return Create(new double[]
                {
                    4.067934061148559026665247110206084571051201477121972612218005e-06,
                     9.421469475576740631603027533116630224451049736050903361458759e-05,
                     1.019122680375098109319314672751485080202557607467199213778085e-03,
                     6.807292884319132011971333979015625113494050642797397817625326e-03,
                     3.123628851149071453063391210769353068187088999495893257051179e-02,
                     1.034044558614783789938787754929279183985553322796063517049140e-01,
                     2.513073789944933128513251971488905042866779761014740192816902e-01,
                     4.435927392240354378183910489448494594782039032807956294826105e-01,
                     5.370084275091661028670690231716974547580034932361053607723887e-01,
                     3.603456405180473278744458573988718422538114217890792270621563e-01,
                    -4.388388187393404111343479394097224312100349011932028865098625e-02,
                    -3.238228649121161212147302807993176715625480327235512530593160e-01,
                    -1.817869767667278325788350264528191676841493369460849123538616e-01,
                     1.660413574907809195438433327470947940538097914525298064477785e-01,
                     2.172992893210892977675493456199559114036326358517672106972956e-01,
                    -6.526287131067753892154895911331108284007380738865652420304233e-02,
                    -1.919195892985939528760786800798636198516495957924798820500876e-01,
                     1.930954466601835091947734585938109944647435243484967057775110e-02,
                     1.552924803962371144206753760712566993987319378965231186477630e-01,
                    -4.752680834111350445288110998030979143710864689041902167119118e-03,
                    -1.205855226433935545076589480704957722635324456812322150437989e-01,
                     4.734229172641948763293980314992213293971770695480616789828384e-03,
                     8.991354757072954417865374195261962983644048998218233900481856e-02,
                    -9.318558949903924837875002823617504227246562152671894579504378e-03,
                    -6.335603744044346612098887534020545705731671718057964802006671e-02,
                     1.322854958503655524455929847605110719648746890497356808289302e-02,
                     4.125469306470509212749750814299126656151504805845417994651417e-02,
                    -1.436683978422007182104025173214012797788904894291716373493525e-02,
                    -2.416949780166026740294880681731084091264533168816746227537030e-02,
                     1.276645671565674419403918018742432714973656598227939824940035e-02,
                     1.228943600811871086161967625814297050611100200023898377949151e-02,
                    -9.577797899235709998147309703713518608283233882793489733491642e-03,
                    -5.085991649233429881797636583578921194675393807761154549733547e-03,
                     6.137754586740521089596801883631921221145712545042519987641234e-03,
                     1.428088794070762107355585870669842132609159040625895090070111e-03,
                    -3.357644380922383229567732565298665639037348585961127075507937e-03,
                     7.615969435172736546769649923895317451534703066016116257300160e-06,
                     1.549637469702362975561719246539787717204438637997824935787688e-03,
                    -3.346692164250854961608526121524596908041109918361306282201310e-04,
                    -5.864810318991817532175809224131456738367101035694188223408841e-04,
                     2.648328819961289039302810122699710966048565368047575218693134e-04,
                     1.700012283661249043584690194716767771204207742625746308522935e-04,
                    -1.365883072261161602559926714744746422567509177443594045709653e-04,
                    -2.976995962848509743944225866488519668585242655980656646544319e-05,
                     5.304143122913310222538317980686374696005605533475685587486683e-05,
                    -2.437001526827789860990429478540556752694389693432668831073769e-06,
                    -1.572442077270281693663288966405861215692805972737981986121447e-05,
                     4.308047861716731191350493437937513220737450410132878032163179e-06,
                     3.353345862871309889390877168046133657377105681618708355266688e-06,
                    -1.895929617693153288493891051875444439753318548105998166574535e-06,
                    -3.903931733287306166657519468494511920760767388397825775326745e-07,
                     5.302368616904760917074352633915743250769600635829229600812520e-07,
                    -3.700308378205124537986402644918879149894035910106489082512364e-08,
                    -9.990396944534900755781728477561240762191443422318249128866740e-08,
                     3.008188650719066928230268918661718274504955045022550217051301e-08,
                     1.084902733789934825266560240100449884702749303326571747323086e-08,
                    -7.458116552893037631192407611262788593505988638365840409367117e-09,
                     5.897951310384361575470355861162022501172491937837712969865619e-11,
                     1.030823345485433383811700481488557422005210168069163779730908e-09,
                    -2.433545573751672936168877250405940817227367937230289801251648e-10,
                    -6.407938256501889018430608323235974406219193176918284664973727e-11,
                     4.000536627253744510742788201354093006471710416671002244302586e-11,
                    -3.125639357108557540598098228678150768528121565391376265627294e-12,
                    -2.567065476155081449204643852428401530283519685638256074752850e-12,
                     8.015088533687900921948605418789324826115616416343391081288979e-13,
                    -2.597954328893848084315198205094389145706680129208998638802995e-14,
                    -3.397720856796267431956783825659069596940335130100871912329556e-14,
                     8.624037434720089202680337663692777682810714650060805832406135e-15,
                    -9.298012529324185420921555664719863501848315099116725184370339e-16,
                    4.014628712333488654318569164614220308046021091178184654250982e-17
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 36-го порядка.
        /// </summary>
        public static WaveletPack D36
        {
            get
            {
                return Create(new double[]
                {
                    2.867925182755946334630479473029238615535511775894262711054705e-06,
                     6.826028678546358691748629102209605362240344266505035981791715e-05,
                     7.602151099668488285869792677106082100141275054892389379198545e-04,
                     5.240297377409884366201603524392995696042174937194435235003941e-03,
                     2.489056564482796484885927333959115579403023347044729739255255e-02,
                     8.565209259526409083864716995521111486437594750377856524772704e-02,
                     2.177569530979008149637945915719999746248969705650625533415876e-01,
                     4.064336977082553467407793990250384445903151630768558142125382e-01,
                     5.322668952607286914777444748641462027213554723153906901129337e-01,
                     4.178753356009697863620634559374236455222275302996931178265919e-01,
                     4.397519752934862993862182898358763783110745559238982179690132e-02,
                    -2.944210395891145711100715969898758940722458887377844633443675e-01,
                    -2.468070369781255270524798278622698446566520718230313889086016e-01,
                     9.811420416311477050518401371401568038943437322299913514049728e-02,
                     2.465372776089742110529709111809595434656418762898152706621356e-01,
                     7.278515095792229009687682299460382878643139026668958884429641e-03,
                    -1.993372056086496198603363400094784142714162256792182570541036e-01,
                    -4.586140074639271639145126228774831743002971373998329604574394e-02,
                     1.541062366276428841776316300420654875883842819413623395358262e-01,
                     5.027618007353842862036816972809884096761706036019748316890913e-02,
                    -1.188037543101356316801816931383547446073152951044444224449501e-01,
                    -3.988085357551317584091699967924044034100374257075864260934102e-02,
                     9.115678225801654406336059281306715151058903055370522031843771e-02,
                     2.503872144956848989919484296709846860569180993040383621980546e-02,
                    -6.820901663681751124880436344265538690580358108714540763125119e-02,
                    -1.131910031681742794381808082173695022123056280821611354577883e-02,
                     4.851308354780908538616267662315735632292989749013261207046367e-02,
                     1.424972661765391603147802607378542396323429657660009755652404e-03,
                    -3.198072067763969654470293513742344601172739688274251641873778e-02,
                     3.984040198717004857397179486790082321314291366656151213429068e-03,
                     1.906359478062535932877576164368198274858108513696832728889209e-02,
                    -5.657813245058818380424016973516714570499161434975761798379020e-03,
                    -9.990263473281372348001743806489172665465685056975652497503772e-03,
                     5.022989106665829004699819220796538830393945994687289792465541e-03,
                     4.413484835350575251918616780287775585471012556848037301025999e-03,
                    -3.484541445404883311209541395428535732697661971818727286003028e-03,
                    -1.503074066296643749549363655363411879858070202740814054964603e-03,
                     1.990793771851737270404293245701878186600899439513475823305914e-03,
                     2.776812795712026068152384207605140383490242756921936501940389e-04,
                    -9.463403823261101964604918059447913047725482130063492242779878e-04,
                     8.614565758992702032613879159402330909634737204578606399403107e-05,
                     3.693507284967510502620040341882236687749563414433432842567511e-04,
                    -1.155118895843527096848376999413102395191976350936666573818799e-04,
                    -1.131899468084665671727391922924411467938450743565106978099456e-04,
                     6.694741196930590257104231749283786251555566773398199990337698e-05,
                     2.375106683660860777161950832380341362257503761490580896617678e-05,
                    -2.731390824654337912922346414722045404779935825834384250023192e-05,
                    -1.183471059985615942783182762352360917304348034947412986608322e-06,
                     8.372218198160788432628056043217491552198857358432112275253310e-06,
                    -1.586145782434577495502614631566211839722879492827911790709498e-06,
                    -1.870811602859180713762972281154953528056257451900381097476968e-06,
                     8.311421279707778528163597405935375886855029592150424544500718e-07,
                     2.548423522556577831218519052844387478819866531902854523544709e-07,
                    -2.455377658434232699135878286794578515387138194247693201846263e-07,
                     2.753249073339512254085076456700241929492720457889076058451072e-09,
                     4.799043465450992009934526867650497683545716858606119786327559e-08,
                    -1.156093688817008406756913949175208452083765368825442482226093e-08,
                    -5.612784343327791397474114357094368557982413895802980814813369e-09,
                     3.138841695782424018351567952158415003571380699236147752239001e-09,
                     1.090815553713751810964713058800448676068475673611349566405716e-10,
                    -4.512545778563249634425200856088490195004077806062978067796020e-10,
                     8.962418203859611987065968320295929679774693465791367610044773e-11,
                     3.037429098112535221800013609576297196061786927734556635696416e-11,
                    -1.599716689261357143200396922409448515398648489795044468046420e-11,
                     8.876846287217374213524399682895564055949886050748321818411161e-13,
                     1.070969357114017002424433471621197579059927261727846375968378e-12,
                    -3.029285026974877268896134589769473854669758797446795757329862e-13,
                     5.542263182639804235231685861028995158694397223907295269180336e-15,
                     1.338071386299105896025578761458472955294763310766371178363783e-14,
                    -3.204628543401749860439316638848579711789176444320134355253750e-15,
                     3.339971984818693213132578777712503670014459411167839211495237e-16,
                    -1.403274175373190617489823209168013922564353495443487431242610e-17
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 37-го порядка.
        /// </summary>
        public static WaveletPack D37
        {
            get
            {
                return Create(new double[]
                {
                    2.022060862498392121815038335333633351464174415618614893795880e-06,
                     4.942343750628132004714286117434454499485737947791397867195910e-05,
                     5.662418377066724013768394373249439163518654840493603575144737e-04,
                     4.024140368257286770702140124893772447952256842478891548092703e-03,
                     1.976228615387959153244055502205017461538589475705618414896893e-02,
                     7.058482597718160832030361890793007659963483925312132741868671e-02,
                     1.873263318620649448028843491747601576761901656888288838192023e-01,
                     3.684409724003061409445838616964941132670287724754729425204047e-01,
                     5.181670408556228873104519667534437205387109579265718071174178e-01,
                     4.622075536616057145505448401528172070050768534504278694229363e-01,
                     1.308789632330201726057701201017649601034381070893275586898075e-01,
                    -2.461804297610834132869018581145720710365433914584680691693717e-01,
                    -2.943759152626617722808219575932673733674290772235644691367427e-01,
                     1.967150045235938977077768648740052380288156507222647187301894e-02,
                     2.515232543602686933435224095078166291442923992611593827552710e-01,
                     8.180602838721862339029076982652411696000045533716726027662147e-02,
                    -1.819622917786080007408824256525225216444443143868752611284260e-01,
                    -1.084517138233017845554078812341876568514835176341639783558543e-01,
                     1.299296469598537527842528895259188653120602318620944502979726e-01,
                     1.017802968388141797470948228505865617480048287983176581607964e-01,
                    -9.660754061668439030915405045955772715988585374771282291315496e-02,
                    -8.233021190655740867404073660920379414988302492018783774702028e-02,
                     7.504761994836017933579005072594245435071674452882148228583865e-02,
                     5.956741087152995245435589042520108066877114768216272503684398e-02,
                    -5.925681563265897095153806724965924334077555174281436189512239e-02,
                    -3.825382947938424882011108885090442116802994193611884738133373e-02,
                     4.580794415126833246633256156110381805848138158784734496981778e-02,
                     2.097280059259754883313769469036393294461497749083921162354229e-02,
                    -3.352358406410096994358662875913243067234786296009238949920582e-02,
                    -8.833493890410232394064187990625563257107429109130726291528648e-03,
                     2.261865154459947356571431658958802912061105608212828675323452e-02,
                     1.690472383484423743663952859090705636512807161536954018400081e-03,
                    -1.376398196289478433857985486097070339786225136728067000591187e-02,
                     1.519305778833399218481261844599507408563295102235964076544334e-03,
                     7.387757452855583640107787619408806919082115520707105052944171e-03,
                    -2.248053187003824706127276829147166466869908326245810952521710e-03,
                    -3.394523276408398601988475786247462646314228994098320665709345e-03,
                     1.816871343801423525477184531347879515909226877688306010517914e-03,
                     1.263934258117477182626760951047019242187910977671449470318766e-03,
                    -1.111484865318630197259018233162929628309920117691177260742614e-03,
                    -3.280788470880198419407186455190899535706232295554613820907245e-04,
                     5.490532773373631230219769273898345809368332716288071475378651e-04,
                     1.534439023195503211083338679106161291342621676983096723309776e-05,
                    -2.208944032455493852493630802748509781675182699536797043565515e-04,
                     4.336726125945695214852398433524024058216834313839357806404424e-05,
                     7.055138782065465075838703109997365141906130284669094131032488e-05,
                    -3.098662927619930052417611453170793938796310141219293329658062e-05,
                    -1.639162496160583099236044020495877311072716199713679670940295e-05,
                     1.354327718416781810683349121150634031343717637827354228989989e-05,
                     1.849945003115590390789683032647334516600314304175482456338006e-06,
                    -4.309941556597092389020622638271988877959028012481278949268461e-06,
                     4.854731396996411681769911684430785681028852413859386141424939e-07,
                     1.002121399297177629772998172241869405763288457224082581829033e-06,
                    -3.494948603445727645895194867933547164628229076947330682199174e-07,
                    -1.509885388671583553484927666148474078148724554849968758642331e-07,
                     1.109031232216439389999036327867142640916239658806376290861690e-07,
                     5.350657515461434290618742656970344024396382191417247602674540e-09,
                    -2.252193836724805775389816424695618411834716065179297102428180e-08,
                     4.224485706362419268050011630338101126995607958955688879525896e-09,
                     2.793974465953982659829387370821677112004867350709951380622807e-09,
                    -1.297205001469435139867686007585972538983682739297235604327668e-09,
                    -1.031411129096974965677950646498153071722880698222864687038596e-10,
                     1.946164894082315021308714557636277980079559327508927751052218e-10,
                    -3.203398244123241367987902201268363088933939831689591684670080e-11,
                    -1.398415715537641487959551682557483348661602836709278513081908e-11,
                     6.334955440973913249611879065201632922100533284261000819747915e-12,
                    -2.096363194234800541614775742755555713279549381264881030843258e-13,
                    -4.421612409872105367333572734854401373201808896976552663098518e-13,
                     1.138052830921439682522395208295427884729893377395129205716662e-13,
                    -4.518889607463726394454509623712773172513778367070839294449849e-16,
                    -5.243025691884205832260354503748325334301994904062750850180233e-15,
                     1.189012387508252879928637969242590755033933791160383262132698e-15,
                    -1.199280335852879554967035114674445327319437557227036460257649e-16,
                    4.906615064935203694857690087429901193139905690549533773201453e-18
                }
            );
            }
        }
        /// <summary>
        /// Возвращает вейвлет Добеши 38-го порядка.
        /// </summary>
        public static WaveletPack D38
        {
            get
            {
                return Create(new double[]
                {
                     1.425776641674131672055420247567865803211784397464191115245081e-06,
                     3.576251994264023012742569014888876217958307227940126418281357e-05,
                     4.211702664727116432247014444906469155300573201130549739553848e-04,
                     3.083088119253751774288740090262741910177322520624582862578292e-03,
                     1.563724934757215617277490102724080070486270026632620664785632e-02,
                     5.788994361285925649727664279317241952513246287766481213301801e-02,
                     1.600719935641106973482800861166599685169395465055048951307626e-01,
                     3.307757814110146511493637534404611754800768677041577030757306e-01,
                     4.965911753117180976599171147718708939352414838951726087564419e-01,
                     4.933560785171007975728485346997317064969513623594359091115804e-01,
                     2.130505713555785138286743353458562451255624665951160445122307e-01,
                    -1.828676677083358907975548507946239135218223185041410632924815e-01,
                    -3.216756378089978628483471725406916361929841940528189059002548e-01,
                    -6.226650604782432226643360160478765847565862101045597180310490e-02,
                     2.321259638353531085028708104285994998671615563662858079262996e-01,
                     1.499851196187170199586403453788927307298226028262603028635758e-01,
                    -1.417956859730596216710053144522330276392591055375830654519080e-01,
                    -1.599125651582443618288533214523534937804208844386102639177693e-01,
                     8.563812155615105741612217814369165313487129645536001850276987e-02,
                     1.414147340733826800884683119379170594092606174915755283496153e-01,
                    -5.658645863072738145681787657843320646815509410635114234947902e-02,
                    -1.147311707107443752394144019458942779715665489230169950201022e-01,
                     4.309589543304764288137871223616030624246568683595408792078602e-02,
                     8.720439826203975011910714164154456762073786124233088471855868e-02,
                    -3.660510340287429567372071039506772372567938710943432838908247e-02,
                    -6.176620870841315993604736705613246241897497782373337911398117e-02,
                     3.198987753153780630818381136366859026137035450576631134176875e-02,
                     4.005498110511594820952087086241114309038577379366732959648548e-02,
                    -2.689149388089451438550851767715967313417890393287236700072071e-02,
                    -2.311413402054931680856913553585621248925303865540203357180768e-02,
                     2.090464525565524340215982365351342094670261491526831672682244e-02,
                     1.129049727868596484270081487761544232851115891449843967151657e-02,
                    -1.470188206539868213708986402816605045648481224662435114088245e-02,
                    -4.131306656031089274123231103326745723188134548520938157995702e-03,
                     9.214785032197180512031534870181734003522861645903894504302286e-03,
                     5.625715748403532005741565594881148757066703437214522101740941e-04,
                    -5.071314509218348093935061417505663002006821323958752649640329e-03,
                     7.169821821064019257784165364894915621888541496773370435889585e-04,
                     2.400697781890973183892306914082592143984140550210130139535193e-03,
                    -8.448626665537775009068937851465856973251363010924003314643612e-04,
                    -9.424614077227377964015942271780098283910230639908018778588910e-04,
                     5.810759750532863662020321063678196633409555706981476723988312e-04,
                     2.817639250380670746018048967535608190123523180612961062603672e-04,
                    -3.031020460726611993600629020329784682496477106470427787747855e-04,
                    -4.555682696668420274688683005987764360677217149927938344795290e-05,
                     1.262043350166170705382346537131817701361522387904917335958705e-04,
                    -1.155409103833717192628479047983460953381959342642374175822863e-05,
                    -4.175141648540397797296325065775711309197411926289412468280801e-05,
                     1.334176149921350382547503457286060922218070031330137601427324e-05,
                     1.037359184045599795632258335010065103524959844966094870217687e-05,
                    -6.456730428469619160379910439617575420986972394137121953806236e-06,
                    -1.550844350118602575853380148525912999401292473185534395740371e-06,
                     2.149960269939665207789548199790770596890252405076394885606038e-06,
                    -8.487087586072593071869805266089426629606479876982221840833098e-08,
                    -5.187733738874144426008474683378542368066310000602823096009187e-07,
                     1.396377545508355481227961581059961184519872502493462010264633e-07,
                     8.400351046895965526933587176781279507953080669259318722910523e-08,
                    -4.884757937459286762082185411608763964041010392101914854918157e-08,
                    -5.424274800287298511126684174854414928447521710664476410973981e-09,
                     1.034704539274858480924046490952803937328239537222908159451039e-08,
                    -1.436329487795135706854539856979275911183628476521636251660849e-09,
                    -1.349197753983448821850381770889786301246741304307934955997111e-09,
                     5.261132557357598494535766638772624572100332209198979659077082e-10,
                     6.732336490189308685740626964182623159759767536724844030164551e-11,
                    -8.278256522538134727330692938158991115335384611795874767521731e-11,
                     1.101692934599454551150832622160224231280195362919498540913658e-11,
                     6.291537317039508581580913620859140835852886308989584198166174e-12,
                    -2.484789237563642857043361214502760723611468591833262675852242e-12,
                     2.626496504065252070488282876470525379851429538389481576454618e-14,
                     1.808661236274530582267084846343959377085922019067808145635263e-13,
                    -4.249817819571463006966616371554206572863122562744916796556474e-14,
                    -4.563397162127373109101691643047923747796563449194075621854491e-16,
                     2.045099676788988907802272564402310095398641092819367167252952e-15,
                    -4.405307042483461342449027139838301611006835285455050155842865e-16,
                     4.304596839558790016251867477122791508849697688058169053134463e-17,
                    -1.716152451088744188732404281737964277713026087224248235541071e-18
                }
            );
            }
        }
        #endregion

        #region Coiflets wavelets
        /// <summary>
        /// Возвращает вейвлет койфлет 1-го порядка.
        /// </summary>
        public static WaveletPack C1
        {
            get
            {
                return Create(new double[] { 
                  -0.015655728135465,
                  -0.072732619512854,
                   0.384864846864203,
                   0.852572020212255,
                   0.337897662457809,
                  -0.072732619512854 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет койфлет 2-го порядка.
        /// </summary>
        public static WaveletPack C2
        {
            get
            {
                return Create(new double[] { 
                  -0.000720549445365,
                  -0.001823208870703,
                   0.005611434819394,
                   0.023680171946334,
                  -0.059434418646457,
                  -0.076488599078306,
                   0.417005184421693,
                   0.812723635445542,
                   0.386110066821162,
                  -0.067372554721963,
                  -0.041464936781759,
                   0.016387336463522 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет койфлет 3-го порядка.
        /// </summary>
        public static WaveletPack C3
        {
            get
            {
                return Create(new double[] { 
                  -0.000034599772836,
                  -0.000070983303138,
                   0.000466216960113,
                   0.001117518770891,
                  -0.002574517688750,
                  -0.009007976136662,
                   0.015880544863616,
                   0.034555027573062,
                  -0.082301927106886,
                  -0.071799821619312,
                   0.428483476377619,
                   0.793777222625621,
                   0.405176902409617,
                  -0.061123390002673,
                  -0.065771911281856,
                   0.023452696141836,
                   0.007782596427325,
                  -0.003793512864491 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет койфлет 4-го порядка.
        /// </summary>
        public static WaveletPack C4
        {
            get
            {
                return Create(new double[] { 
                  -0.000001784985003,
                  -0.000003259680237,
                   0.000031229875865,
                   0.000062339034461,
                  -0.000259974552488,
                  -0.000589020756244,
                   0.001266561929299,
                   0.003751436157278,
                  -0.005658286686611,
                  -0.015211731527946,
                   0.025082261844864,
                   0.039334427123337,
                  -0.096220442033988,
                  -0.066627474263425,
                   0.434386056491469,
                   0.782238930920499,
                   0.415308407030430,
                  -0.056077313316755,
                  -0.081266699680879,
                   0.026682300156053,
                   0.016068943964776,
                  -0.007346166327642,
                  -0.001629492012602,
                   0.000892313668582 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет койфлет 5-го порядка.
        /// </summary>
        public static WaveletPack C5
        {
            get
            {
                return Create(new double[] {
                  -0.000000095176573,
                  -0.000000167442886,
                   0.000002063761851,
                   0.000003734655175,
                  -0.000021315026810,
                  -0.000041340432273,
                   0.000140541149702,
                   0.000302259581813,
                  -0.000638131343045,
                  -0.001662863702013,
                   0.002433373212658,
                   0.006764185448053,
                  -0.009164231162482,
                  -0.019761778942573,
                   0.032683574267112,
                   0.041289208750182,
                  -0.105574208703339,
                  -0.062035963962904,
                   0.437991626171837,
                   0.774289603652956,
                   0.421566206690851,
                  -0.052043163176244,
                  -0.091920010559696,
                   0.028168028970936,
                   0.023408156785839,
                  -0.010131117519850,
                  -0.004159358781386,
                   0.002178236358109,
                   0.000358589687896,
                  -0.000212080839804 });
            }
        }
        #endregion

        #region Symlets wavelets
        /// <summary>
        /// Возвращает вейвлет симлет 1-го порядка.
        /// <remarks>
        /// Вейвлет Хаара.
        /// </remarks>
        /// </summary>
        public static WaveletPack S1
        {
            get
            {
                // Haar's wavelet:
                return WaveletPack.Haar;
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 2-го порядка.
        /// </summary>
        public static WaveletPack S2
        {
            get
            {
                return Create(new double[] { -0.129409522550921, 0.224143868041857, 0.836516303737469, 0.482962913144690 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 3-го порядка.
        /// </summary>
        public static WaveletPack S3
        {
            get
            {
                return Create(new double[] { 0.035226291882101, -0.085441273882241, -0.135011020010391, 0.459877502119331, 0.806891509313339, 0.332670552950957 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 4-го порядка.
        /// </summary>
        public static WaveletPack S4
        {
            get
            {
                return Create(new double[] { -0.075765714789273, -0.029635527645999, 0.497618667632015, 0.803738751805916, 0.297857795605277, -0.099219543576847, -0.012603967262038, 0.032223100604043 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 5-го порядка.
        /// </summary>
        public static WaveletPack S5
        {
            get
            {
                return Create(new double[] { 
                   0.027333068345078,
                   0.029519490925775,
                  -0.039134249302383,
                   0.199397533977394,
                   0.723407690402421,
                   0.633978963458212,
                   0.016602105764522,
                  -0.175328089908450,
                  -0.021101834024759,
                   0.019538882735287 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 6-го порядка.
        /// </summary>
        public static WaveletPack S6
        {
            get
            {
                return Create(new double[] { 
                   0.015404109327027,
                   0.003490712084217,
                  -0.117990111148191,
                  -0.048311742585633,
                   0.491055941926747,
                   0.787641141030194,
                   0.337929421727622,
                  -0.072637522786463,
                  -0.021060292512301,
                   0.044724901770666,
                   0.001767711864243,
                  -0.007800708325034 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 7-го порядка.
        /// </summary>
        public static WaveletPack S7
        {
            get
            {
                return Create(new double[] { 
                   0.002681814568258,
                  -0.001047384888683,
                  -0.012636303403252,
                   0.030515513165964,
                   0.067892693501373,
                  -0.049552834937127,
                   0.017441255086856,
                   0.536101917091763,
                   0.767764317003164,
                   0.288629631751515,
                  -0.140047240442962,
                  -0.107808237703818,
                   0.004010244871534,
                   0.010268176708511 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 8-го порядка.
        /// </summary>
        public static WaveletPack S8
        {
            get
            {
                return Create(new double[] { 
                  -0.003382415951006,
                  -0.000542132331791,
                   0.031695087811493,
                   0.007607487324918,
                  -0.143294238350810,
                  -0.061273359067659,
                   0.481359651258372,
                   0.777185751700524,
                   0.364441894835331,
                  -0.051945838107709,
                  -0.027219029917056,
                   0.049137179673608,
                   0.003808752013891,
                  -0.014952258337048,
                  -0.000302920514721,
                   0.001889950332759 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 9-го порядка.
        /// </summary>
        public static WaveletPack S9
        {
            get
            {
                return Create(new double[] { 
                   0.001400915525915,
                   0.000619780888985,
                  -0.013271967781817,
                  -0.011528210207679,
                   0.030224878858275,
                   0.000583462746123,
                  -0.054568958430835,
                   0.238760914607305,
                   0.717897082764415,
                   0.617338449140936,
                   0.035272488035270,
                  -0.191550831297285,
                  -0.018233770779395,
                   0.062077789302886,
                   0.008859267493400,
                  -0.010264064027633,
                  -0.000473154498680,
                   0.001069490032909 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 10-го порядка.
        /// </summary>
        public static WaveletPack S10
        {
            get
            {
                return Create(new double[] { 
                   0.000770159809115,
                   0.000095632670723,
                  -0.008641299277023,
                  -0.001465382581305,
                   0.045927239231093,
                   0.011609893903711,
                  -0.159494278884922,
                  -0.070880535783246,
                   0.471690666938446,
                   0.769510037021116,
                   0.383826761067085,
                  -0.035536740473823,
                  -0.031990056882430,
                   0.049994972077377,
                   0.005764912033581,
                  -0.020354939812312,
                  -0.000804358932017,
                   0.004593173585312,
                   0.000057036083618,
                  -0.000459329421005 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 11-го порядка.
        /// </summary>
        public static WaveletPack S11
        {
            get
            {
                return Create(new double[] { 0.00048926361026192387, 0.00011053509764272153, -0.0063896036664548919, -0.0020034719001093887, 0.043000190681552281, 0.035266759564466552, -0.14460234370531561, -0.2046547944958006, 0.23768990904924897, 0.73034354908839572, 0.57202297801008706, 0.097198394458909473, -0.022832651022562687, 0.069976799610734136, 0.0370374159788594, -0.024080841595864003, -0.0098579348287897942, 0.0065124956747714497, 0.00058835273539699145, -0.0017343662672978692, -3.8795655736158566e-005, 0.00017172195069934854 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 12-го порядка.
        /// </summary>
        public static WaveletPack S12
        {
            get
            {
                return Create(new double[] { -0.00017906658697508691, -1.8158078862617515e-005, 0.0023502976141834648, 0.00030764779631059454, -0.014589836449234145, -0.0026043910313322326, 0.057804179445505657, 0.01530174062247884, -0.17037069723886492, -0.07833262231634322, 0.46274103121927235, 0.76347909778365719, 0.39888597239022, -0.022162306170337816, -0.035848830736954392, 0.049179318299660837, 0.0075537806116804775, -0.024220722675013445, -0.0014089092443297553, 0.007414965517654251, 0.00018021409008538188, -0.0013497557555715387, -1.1353928041541452e-005, 0.00011196719424656033 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 13-го порядка.
        /// </summary>
        public static WaveletPack S13
        {
            get
            {
                return Create(new double[] { 7.0429866906944016e-005, 3.6905373423196241e-005, -0.0007213643851362283, 0.00041326119884196064, 0.0056748537601224395, -0.0014924472742598532, -0.020749686325515677, 0.017618296880653084, 0.092926030899137119, 0.0088197576704205465, -0.14049009311363403, 0.11023022302137217, 0.64456438390118564, 0.69573915056149638, 0.19770481877117801, -0.12436246075153011, -0.059750627717943698, 0.013862497435849205, -0.017211642726299048, -0.02021676813338983, 0.0052963597387250252, 0.0075262253899680996, -0.00017094285853022211, -0.0011360634389281183, -3.5738623648689009e-005, 6.8203252630753188e-005 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 14-го порядка.
        /// </summary>
        public static WaveletPack S14
        {
            get
            {
                return Create(new double[] { 4.4618977991475265e-005, 1.9329016965523917e-005, -0.00060576018246643346, -7.3214213567023991e-005, 0.0045326774719456481, 0.0010131419871842082, -0.019439314263626713, -0.0023650488367403851, 0.069827616361807551, 0.025898587531046669, -0.15999741114652205, -0.058111823317717831, 0.47533576263420663, 0.75997624196109093, 0.39320152196208885, -0.035318112114979733, -0.057634498351326995, 0.037433088362853452, 0.0042805204990193782, -0.029196217764038187, -0.0027537747912240711, 0.010037693717672269, 0.00036647657366011829, -0.002579441725933078, -6.2865424814776362e-005, 0.00039843567297594335, 1.1210865808890361e-005, -2.5879090265397886e-005 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 15-го порядка.
        /// </summary>
        public static WaveletPack S15
        {
            get
            {
                return Create(new double[] { 2.8660708525318081e-005, 2.1717890150778919e-005, -0.00040216853760293483, -0.00010815440168545525, 0.003481028737064895, 0.0015261382781819983, -0.017171252781638731, -0.0087447888864779517, 0.067969829044879179, 0.068393310060480245, -0.13405629845625389, -0.1966263587662373, 0.2439627054321663, 0.72184302963618119, 0.57864041521503451, 0.11153369514261872, -0.04108266663538248, 0.040735479696810677, 0.021937642719753955, -0.038876716876833493, -0.019405011430934468, 0.010079977087905669, 0.003423450736351241, -0.0035901654473726417, -0.00026731644647180568, 0.0010705672194623959, 5.5122547855586653e-005, -0.00016066186637495343, -7.3596667989194696e-006, 9.7124197379633478e-006 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 16-го порядка.
        /// </summary>
        public static WaveletPack S16
        {
            get
            {
                return Create(new double[] { -1.0797982104319795e-005, -5.3964831793152419e-006, 0.00016545679579108483, 3.656592483348223e-005, -0.0013387206066921965, -0.00022211647621176323, 0.0069377611308027096, 0.001359844742484172, -0.024952758046290123, -0.0035102750683740089, 0.078037852903419913, 0.03072113906330156, -0.15959219218520598, -0.054040601387606135, 0.47534280601152273, 0.75652498787569711, 0.39712293362064416, -0.034574228416972504, -0.066983049070217779, 0.032333091610663785, 0.0048692744049046071, -0.031051202843553064, -0.0031265171722710075, 0.012666731659857348, 0.00071821197883178923, -0.0038809122526038786, -0.0001084456223089688, 0.00085235471080470952, 2.8078582128442894e-005, -0.00010943147929529757, -3.1135564076219692e-006, 6.2300067012207606e-006 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 17-го порядка.
        /// </summary>
        public static WaveletPack S17
        {
            get
            {
                return Create(new double[] { 3.7912531943321266e-006, -2.4527163425832999e-006, -7.6071244056051285e-005, 2.5207933140828779e-005, 0.0007198270642148971, 5.8400428694052584e-005, -0.0039323252797979023, -0.0019054076898526659, 0.012396988366648726, 0.0099529825235095976, -0.01803889724191924, -0.0072616347509287674, 0.016158808725919346, -0.086070874720733381, -0.15507600534974825, 0.18053958458111286, 0.68148899534492502, 0.65071662920454565, 0.14239835041467819, -0.11856693261143636, 0.0172711782105185, 0.10475461484223211, 0.017903952214341119, -0.033291383492359328, -0.0048192128031761478, 0.010482366933031529, 0.0008567700701915741, -0.0027416759756816018, -0.00013864230268045499, 0.0004759963802638669, -1.3506383399901165e-005, -6.2937025975541919e-005, 2.7801266938414138e-006, 4.297343327345983e-006 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 18-го порядка.
        /// </summary>
        public static WaveletPack S18
        {
            get
            {
                return Create(new double[] { -1.5131530692371587e-006, 7.8472980558317646e-007, 2.9557437620930811e-005, -9.858816030140058e-006, -0.00026583011024241041, 4.7416145183736671e-005, 0.0014280863270832796, -0.00018877623940755607, -0.0052397896830266083, 0.0010877847895956929, 0.015012356344250213, -0.0032607442000749834, -0.031712684731814537, 0.0062779445543116943, 0.028529597039037808, -0.073799207290607169, -0.032480573290138676, 0.40148386057061813, 0.75362914010179283, 0.47396905989393956, -0.052029158983952786, -0.15993814866932407, 0.033995667103947358, 0.084219929970386548, -0.0050770851607570529, -0.030325091089369604, 0.0016429863972782159, 0.0095021643909623654, -0.00041152110923597756, -0.0023138718145060992, 7.0212734590362685e-005, 0.00039616840638254753, -1.4020992577726755e-005, -4.5246757874949856e-005, 1.354915761832114e-006, 2.6126125564836423e-006 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 19-го порядка.
        /// </summary>
        public static WaveletPack S19
        {
            get
            {
                return Create(new double[] { 1.7509367995348687e-006, 2.0623170632395688e-006, -2.8151138661550245e-005, -1.6821387029373716e-005, 0.00027621877685734072, 0.00012930767650701415, -0.0017049602611649971, -0.00061792232779831076, 0.0082622369555282547, 0.0043193518748949689, -0.027709896931311252, -0.016908234861345205, 0.084072676279245043, 0.093630843415897141, -0.11624173010739675, -0.17659686625203097, 0.25826616923728363, 0.71955552571639425, 0.57814494533860505, 0.10902582508127781, -0.067525058040294086, 0.0089545911730436242, 0.0070155738571741596, -0.046635983534938946, -0.022651993378245951, 0.015797439295674631, 0.0079684383206133063, -0.005122205002583014, -0.0011607032572062486, 0.0021214250281823303, 0.00015915804768084938, -0.00063576451500433403, -4.6120396002105868e-005, 0.0001155392333357879, 8.8733121737292863e-006, -1.1880518269823984e-005, -6.4636513033459633e-007, 5.4877327682158382e-007 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет симлет 20-го порядка.
        /// </summary>
        public static WaveletPack S20
        {
            get
            {
                return Create(new double[] { -6.3291290447763946e-007, -3.2567026420174407e-007, 1.22872527779612e-005, 4.5254222091516362e-006, -0.00011739133516291466, -2.6615550335516086e-005, 0.00074761085978205719, 0.00012544091723067259, -0.0034716478028440734, -0.0006111263857992088, 0.012157040948785737, 0.0019385970672402002, -0.035373336756604236, -0.0068437019650692274, 0.088919668028199561, 0.036250951653933078, -0.16057829841525254, -0.051088342921067398, 0.47199147510148703, 0.75116272842273002, 0.40583144434845059, -0.029819368880333728, -0.078994344928398158, 0.025579349509413946, 0.0081232283560096815, -0.031629437144957966, -0.0033138573836233591, 0.017004049023390339, 0.0014230873594621453, -0.0066065857990888609, -0.0003052628317957281, 0.0020889947081901982, 7.2159911880740349e-005, -0.00049473109156726548, -1.928412300645204e-005, 7.992967835772481e-005, 3.0256660627369661e-006, -7.919361411976999e-006, -1.9015675890554106e-007, 3.695537474835221e-007 });
            }
        }
        #endregion

        #region Meyer wavelet
        /// <summary>
        /// Возвращает вейвлет Мейера.
        /// </summary>
        public static WaveletPack Meyer
        {
            get
            {
                double[] lp = new double[] {
               0.000000000000000,
              -0.000001509740857,
               0.000001278766757,
               0.000000449585560,
              -0.000002096568870,
               0.000001723223554,
               0.000000698082276,
              -0.000002879408033,
               0.000002383148395,
               0.000000982515602,
              -0.000004217789186,
               0.000003353501538,
               0.000001674721859,
              -0.000006034501342,
               0.000004837555802,
               0.000002402288023,
              -0.000009556309846,
               0.000007216527695,
               0.000004849078300,
              -0.000014206928581,
               0.000010503914271,
               0.000006187580298,
              -0.000024438005846,
               0.000020106387691,
               0.000014993523600,
              -0.000046428764284,
               0.000032341311914,
               0.000037409665760,
              -0.000102779005085,
               0.000024461956845,
               0.000149713515389,
              -0.000075592870255,
              -0.000139913148217,
              -0.000093512893880,
               0.000161189819725,
               0.000859500213762,
              -0.000578185795273,
              -0.002702168733939,
               0.002194775336459,
               0.006045510596456,
              -0.006386728618548,
              -0.011044641900539,
               0.015250913158586,
               0.017403888210177,
              -0.032094063354505,
              -0.024321783959519,
               0.063667300884468,
               0.030621243943425,
              -0.132696615358862,
              -0.035048287390595,
               0.444095030766529,
               0.743751004903787,
               0.444095030766529,
              -0.035048287390595,
              -0.132696615358862,
               0.030621243943425,
               0.063667300884468,
              -0.024321783959519,
              -0.032094063354505,
               0.017403888210177,
               0.015250913158586,
              -0.011044641900539,
              -0.006386728618548,
               0.006045510596456,
               0.002194775336459,
              -0.002702168733939,
              -0.000578185795273,
               0.000859500213762,
               0.000161189819725,
              -0.000093512893880,
              -0.000139913148217,
              -0.000075592870255,
               0.000149713515389,
               0.000024461956845,
              -0.000102779005085,
               0.000037409665760,
               0.000032341311914,
              -0.000046428764284,
               0.000014993523600,
               0.000020106387691,
              -0.000024438005846,
               0.000006187580298,
               0.000010503914271,
              -0.000014206928581,
               0.000004849078300,
               0.000007216527695,
              -0.000009556309846,
               0.000002402288023,
               0.000004837555802,
              -0.000006034501342,
               0.000001674721859,
               0.000003353501538,
              -0.000004217789186,
               0.000000982515602,
               0.000002383148395,
              -0.000002879408033,
               0.000000698082276,
               0.000001723223554,
              -0.000002096568870,
               0.000000449585560,
               0.000001278766757,
              -0.000001509740857 };
                            double[] hp = new double[] {
               0.000001509740857,
               0.000001278766757,
              -0.000000449585560,
              -0.000002096568870,
              -0.000001723223554,
               0.000000698082276,
               0.000002879408033,
               0.000002383148395,
              -0.000000982515602,
              -0.000004217789186,
              -0.000003353501538,
               0.000001674721859,
               0.000006034501342,
               0.000004837555802,
              -0.000002402288023,
              -0.000009556309846,
              -0.000007216527695,
               0.000004849078300,
               0.000014206928581,
               0.000010503914271,
              -0.000006187580298,
              -0.000024438005846,
              -0.000020106387691,
               0.000014993523600,
               0.000046428764284,
               0.000032341311914,
              -0.000037409665760,
              -0.000102779005085,
              -0.000024461956845,
               0.000149713515389,
               0.000075592870255,
              -0.000139913148217,
               0.000093512893880,
               0.000161189819725,
              -0.000859500213762,
              -0.000578185795273,
               0.002702168733939,
               0.002194775336459,
              -0.006045510596456,
              -0.006386728618548,
               0.011044641900539,
               0.015250913158586,
              -0.017403888210177,
              -0.032094063354505,
               0.024321783959519,
               0.063667300884468,
              -0.030621243943425,
              -0.132696615358862,
               0.035048287390595,
               0.444095030766529,
              -0.743751004903787,
               0.444095030766529,
               0.035048287390595,
              -0.132696615358862,
              -0.030621243943425,
               0.063667300884468,
               0.024321783959519,
              -0.032094063354505,
              -0.017403888210177,
               0.015250913158586,
               0.011044641900539,
              -0.006386728618548,
              -0.006045510596456,
               0.002194775336459,
               0.002702168733939,
              -0.000578185795273,
              -0.000859500213762,
               0.000161189819725,
               0.000093512893880,
              -0.000139913148217,
               0.000075592870255,
               0.000149713515389,
              -0.000024461956845,
              -0.000102779005085,
              -0.000037409665760,
               0.000032341311914,
               0.000046428764284,
               0.000014993523600,
              -0.000020106387691,
              -0.000024438005846,
              -0.000006187580298,
               0.000010503914271,
               0.000014206928581,
               0.000004849078300,
              -0.000007216527695,
              -0.000009556309846,
              -0.000002402288023,
               0.000004837555802,
               0.000006034501342,
               0.000001674721859,
              -0.000003353501538,
              -0.000004217789186,
              -0.000000982515602,
               0.000002383148395,
               0.000002879408033,
               0.000000698082276,
              -0.000001723223554,
              -0.000002096568870,
              -0.000000449585560,
               0.000001278766757,
               0.000001509740857,
               0.000000000000000
                };

                double[] ilp = Matrice.Flip(lp);
                double[] ihp = Matrice.Flip(hp);
                return new WaveletPack(lp, hp, ilp, ihp);
            }
        }
        #endregion

        #region Fejer-Korovkin wavelets
        /// <summary>
        /// Возвращает вейвлет Фейера-Коровкина 4-го порядка.
        /// </summary>
        public static WaveletPack F4
        {
            get
            {
                return Create(new double[] { 
                  -0.046165714815218,
                   0.053179228779060,
                   0.753272492839488,
                   0.653927555569765 }, new double[] {

                  -0.653927555569765,
                   0.753272492839488,
                  -0.053179228779060,
                  -0.046165714815218 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Фейера-Коровкина 6-го порядка.
        /// </summary>
        public static WaveletPack F6
        {
            get
            {
                return Create(new double[] {
                   0.040625814423238,
                  -0.077177757406970,
                  -0.146438681272577,
                   0.356369511070187,
                   0.812919643136907,
                   0.427915032422310}, new double[] { 
                  -0.427915032422310,
                   0.812919643136907,
                  -0.356369511070187,
                  -0.146438681272577,
                   0.077177757406970,
                   0.040625814423238 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Фейера-Коровкина 8-го порядка.
        /// </summary>
        public static WaveletPack F8
        {
            get
            {
                return Create(new double[] { -0.019000178853736,
                   0.042581631677582,
                   0.043106668106516,
                  -0.159978097434030,
                  -0.099683328450573,
                   0.475265135079471,
                   0.782683620384065,
                   0.349238111863800 }, new double[] { -0.349238111863800,
                   0.782683620384065,
                  -0.475265135079471,
                  -0.099683328450573,
                   0.159978097434030,
                   0.043106668106516,
                  -0.042581631677582,
                  -0.019000178853736 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Фейера-Коровкина 14-го порядка.
        /// </summary>
        public static WaveletPack F14
        {
            get
            {
                return Create(new double[] { 
                   0.003514100970436,
                  -0.009270613374448,
                  -0.003297479152709,
                   0.029779711590379,
                  -0.005074372549973,
                  -0.063997373039142,
                   0.022226739622463,
                   0.124282560921513,
                  -0.048575339085855,
                  -0.245613928162192,
                   0.051421654142119,
                   0.611554653959510,
                   0.686891477239598,
                   0.260371769291396 }, new double[] {
                  -0.260371769291396,
                   0.686891477239598,
                  -0.611554653959510,
                   0.051421654142119,
                   0.245613928162192,
                  -0.048575339085855,
                  -0.124282560921513,
                   0.022226739622463,
                   0.063997373039142,
                  -0.005074372549973,
                  -0.029779711590379,
                  -0.003297479152709,
                   0.009270613374448,
                   0.003514100970436 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Фейера-Коровкина 22-го порядка.
        /// </summary>
        public static WaveletPack F22
        {
            get
            {
                return Create(new double[] { 
                   0.000880577399983,
                  -0.002676991599949,
                   0.000361285599993,
                   0.007173803199864,
                  -0.004838432599908,
                  -0.012885990599755,
                   0.012964249399753,
                   0.020284486099614,
                  -0.025970873099506,
                  -0.029742880699434,
                   0.044775212199148,
                   0.043542367599172,
                  -0.071841681898633,
                  -0.066084516798743,
                   0.110155264897904,
                   0.111549143697878,
                  -0.164465715296871,
                  -0.228028855795662,
                   0.215629849095898,
                   0.670084962887252,
                   0.589452190888786,
                   0.193896107796311
                                }, new double[] { 
                   -0.193896107796311,
                   0.589452190888786,
                  -0.670084962887252,
                   0.215629849095898,
                   0.228028855795662,
                  -0.164465715296871,
                  -0.111549143697878,
                   0.110155264897904,
                   0.066084516798743,
                  -0.071841681898633,
                  -0.043542367599172,
                   0.044775212199148,
                   0.029742880699434,
                  -0.025970873099506,
                  -0.020284486099614,
                   0.012964249399753,
                   0.012885990599755,
                  -0.004838432599908,
                  -0.007173803199864,
                   0.000361285599993,
                   0.002676991599949,
                   0.000880577399983 });
            }
        }
        #endregion

        #region Legendre wavelets
        /// <summary>
        /// Возвращает вейвлет Лежандра 1-го порядка.
        /// <remarks>
        /// Вейвлет Хаара.
        /// </remarks>
        /// </summary>
        public static WaveletPack L1
        {
            get
            {
                // Haar's wavelet:
                return WaveletPack.Haar;
            }
        }
        /// <summary>
        /// Возвращает вейвлет Лежандра 2-го порядка.
        /// <remarks>
        /// Неортогональный вейвлет.
        /// </remarks>
        /// </summary>
        public static WaveletPack L2
        {
            get
            {
                return Create(new double[] {  0.441941738001176, 0.265165042944955, 0.265165042944955, 0.441941738001176 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Лежандра 3-го порядка.
        /// <remarks>
        /// Неортогональный вейвлет.
        /// </remarks>
        /// </summary>
        public static WaveletPack L3
        {
            get
            {
                return Create(new double[] { 0.348029118865254, 0.193349510480697, 0.165728151840597, 0.165728151840597, 0.193349510480697, 0.348029118865254 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Лежандра 4-го порядка.
        /// <remarks>
        /// Неортогональный вейвлет.
        /// </remarks>
        /// </summary>
        public static WaveletPack L4
        {
            get
            {
                return Create(new double[] { 
                0.209472656663610,
                0.112792968646356,
                0.092285156550895,
                0.085449218714337,
                0.085449218714337,
                0.092285156550895,
                0.112792968646356,
                0.209472656663610 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Лежандра 5-го порядка.
        /// <remarks>
        /// Неортогональный вейвлет.
        /// </remarks>
        /// </summary>
        public static WaveletPack L5
        {
            get
            {
                return Create(new double[] {
                0.185470581656607,
                0.098190308518174,
                0.078552246248854,
                0.070495604520089,
                0.067291259631474,
                0.067291259631474,
                0.070495604520089,
                0.078552246248854,
                0.098190308518174,
                0.185470581656607 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Лежандра 6-го порядка.
        /// <remarks>
        /// Неортогональный вейвлет.
        /// </remarks>
        /// </summary>
        public static WaveletPack L6
        {
            get
            {
                return Create(new double[] { 
                0.204303513760092,
                0.105674265879069,
                0.082191094112372,
                0.071232282506865,
                0.065038168525027,
                0.061321700135924,
                0.059170059053587,
                0.058175612360798,
                0.058175612360798,
                0.059170059053587,
                0.061321700135924,
                0.065038168525027,
                0.071232282506865,
                0.082191094112372,
                0.105674265879069,
                0.204303513760092 });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Лежандра 7-го порядка.
        /// <remarks>
        /// Неортогональный вейвлет.
        /// </remarks>
        /// </summary>
        public static WaveletPack L7
        {
            get
            {
                return Create(new double[] { 
                0.204303513760092,
                0.105674265879069,
                0.082191094112372,
                0.071232282506865,
                0.065038168525027,
                0.061321700135924,
                0.059170059053587,
                0.058175612360798,
                0.058175612360798,
                0.059170059053587,
                0.061321700135924,
                0.065038168525027,
                0.071232282506865,
                0.082191094112372,
                0.105674265879069,
                0.204303513760092
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Лежандра 8-го порядка.
        /// <remarks>
        /// Неортогональный вейвлет.
        /// </remarks>
        /// </summary>
        public static WaveletPack L8
        {
            get
            {
                return Create(new double[] { 
                0.192098002188675,
                0.098959551600650,
                0.076613845944306,
                0.066046417942185,
                0.059931004945093,
                0.056095417347632,
                0.053656492922234,
                0.052196444698305,
                0.051509660166010,
                0.051509660166010,
                0.052196444698305,
                0.053656492922234,
                0.056095417347632,
                0.059931004945093,
                0.066046417942185,
                0.076613845944306,
                0.098959551600650,
                0.192098002188675
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Лежандра 9-го порядка.
        /// <remarks>
        /// Неортогональный вейвлет.
        /// </remarks>
        /// </summary>
        public static WaveletPack L9
        {
            get
            {
                return Create(new double[] { 
                0.181847075181813,
                0.093380945787564,
                0.072036729607550,
                0.061849710911572,
                0.055992816153682,
                0.052011550417160,
                0.049443084029449,
                0.047747894516504,
                0.046709890045993,
                0.046215608263808,
                0.046215608263808,
                0.046709890045993,
                0.047747894516504,
                0.049443084029449,
                0.052011550417160,
                0.055992816153682,
                0.061849710911572,
                0.072036729607550,
                0.093380945787564,
                0.181847075181813
                });
            }
        }
        #endregion

        #region Fbsp wavelets
        /// <summary>
        /// Возвращает B-spline вейвлет 1-0-0.
        /// <remarks>
        /// Вейвлет Хаара (с задержкой).
        /// </remarks>
        /// </summary>
        public static WaveletPack Fbsp100
        {
            get
            {
                return Create(new double[] { 0, 0, 0, 0, 0.707106781186548, -0.707106781186548, 0, 0, 0, 0 });
            }
        }
        /// <summary>
        /// Возвращает B-spline вейвлет 1-0-3.
        /// </summary>
        public static WaveletPack Fbsp103
        {
            get
            {
                return Create(new double[] { -0.044194173824159, 0.044194173824159, 0.707106781186548, 0.707106781186548, 0.044194173824159, -0.044194173824159 });
            }
        }
        /// <summary>
        /// Возвращает B-spline вейвлет 1-0-5.
        /// </summary>
        public static WaveletPack Fbsp105
        {
            get
            {
                return Create(new double[] { 0.008286407592030, -0.008286407592030, -0.060766989008219, 0.060766989008219, 0.707106781186548, 0.707106781186548, 0.060766989008219, -0.060766989008219, -0.008286407592030, 0.008286407592030 });
            }
        }
        #endregion

        #region Haar wavelet
        /// <summary>
        /// Возвращает вейвлет Хаара.
        /// </summary>
        public static WaveletPack Haar
        {
            get
            {
                return Create(new double[] { 0.707106781186548, 0.707106781186548 });
            }
        }
        #endregion

        #region Cohen-Daubechies-Feaveau wavelets
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 1/1).
        /// </summary>
        public static WaveletPack CDF11
        {
            get
            {
                return WaveletPack.Bior11;
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 1/3).
        /// </summary>
        public static WaveletPack CDF13
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.707106781186548,
                   0.707106781186548 }, new double[] {
                   0.088388347648318,
                   0.088388347648318,
                  -0.707106781186548,
                   0.707106781186548,
                  -0.088388347648318,
                  -0.088388347648318,
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 1/5).
        /// </summary>
        public static WaveletPack CDF15
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.707106781186548,
                   0.707106781186548 }, new double[] {
                  -0.016572815184060,
                  -0.016572815184060,
                   0.121533978016438,
                   0.121533978016438,
                  -0.707106781186548,
                   0.707106781186548,
                  -0.121533978016438,
                  -0.121533978016438,
                   0.016572815184060,
                   0.016572815184060
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 3/1).
        /// </summary>
        public static WaveletPack CDF31
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.176776695296637,
                   0.530330085889911,
                   0.530330085889911,
                   0.176776695296637 }, new double[] {
                   0.353553390593274,
                   1.060660171779821,
                  -1.060660171779821,
                  -0.353553390593274
                });
            }
        }
        /// <summary>
        /// Возвращает лифтинговый вейвлет Коэна-Добеши-Фево (CDF 5/1).
        /// </summary>
        public static WaveletPack CDF51
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.044194173824159,
                   0.220970869120796,
                   0.441941738241592,
                   0.441941738241592,
                   0.220970869120796,
                   0.044194173824159 }, new double[] {
                  -0.265165042944955,
                  -1.325825214724777,
                  -1.767766952966369,
                   1.767766952966369,
                   1.325825214724777,
                   0.265165042944955
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 5/3).
        /// </summary>
        public static WaveletPack CDF53
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.044194173824159,
                   0.220970869120796,
                   0.441941738241592,
                   0.441941738241592,
                   0.220970869120796,
                   0.044194173824159 }, new double[] {
                  -0.055242717280199,
                  -0.276213586400995,
                  -0.817592215746946,
                  -1.878252387526767,
                  -2.099223256647564,
                   1.436310649285175,
                   0.773398041922786,
                  -0.287262129857035,
                  -0.276213586400995,
                  -0.055242717280199
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 5/5).
        /// </summary>
        public static WaveletPack CDF55
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.044194173824159,
                   0.220970869120796,
                   0.441941738241592,
                   0.441941738241592,
                   0.220970869120796,
                   0.044194173824159 }, new double[] {
                  -0.012084344405044,
                  -0.060421722025218,
                  -0.041432037960149,
                   0.276213586400995,
                   0.468527295932688,
                  -0.543795498226959,
                  -1.450121328605225,
                   1.450121328605224,
                   0.543795498226959,
                  -0.468527295932688,
                  -0.276213586400995,
                   0.041432037960149,
                   0.060421722025218,
                   0.012084344405044
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 2/2).
        /// </summary>
        public static WaveletPack CDF22
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.353553390593274,
                   0.707106781186548,
                   0.353553390593274,
                   }, new double[] {
                   0.176776695296637,
                   0.353553390593274,
                  -1.060660171779821,
                   0.353553390593274,
                   0.176776695296637,
                   0.000000000000000
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 2/4).
        /// </summary>
        public static WaveletPack CDF24
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.353553390593274,
                   0.707106781186548,
                   0.353553390593274,
                   }, new double[] {
                  -0.033145630368119,
                  -0.066291260736239,
                   0.176776695296637,
                   0.419844651329513,
                  -0.994368911043582,
                   0.419844651329513,
                   0.176776695296637,
                  -0.066291260736239,
                  -0.033145630368119,
                   0.000000000000000
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 2/6).
        /// </summary>
        public static WaveletPack CDF26
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.353553390593274,
                   0.707106781186548,
                   0.353553390593274,
                   }, new double[] {
                   0.006905339660025,
                   0.013810679320050,
                  -0.046956309688169,
                  -0.107723298696388,
                   0.169871355636612,
                   0.447466009969612,
                  -0.966747552403483,
                   0.447466009969612,
                   0.169871355636612,
                  -0.107723298696388,
                  -0.046956309688169,
                   0.013810679320050,
                   0.006905339660025,
                   0.000000000000000
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 4/2).
        /// </summary>
        public static WaveletPack CDF42
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.088388347648318,
                   0.353553390593274,
                   0.530330085889911,
                   0.353553390593274,
                   0.088388347648318
                                   }, new double[] {
                  -0.132582521472478,
                  -0.530330085889911,
                  -0.220970869120796,
                   1.767766952966369,
                  -0.220970869120796,
                  -0.530330085889911,
                  -0.132582521472478,
                   0.000000000000000
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 4/4).
        /// </summary>
        public static WaveletPack CDF44
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.088388347648318,
                   0.353553390593274,
                   0.530330085889911,
                   0.353553390593274,
                   0.088388347648318
                                   }, new double[] {
                   0.027621358640100,
                   0.110485434560398,
                   0.005524271728020,
                  -0.530330085889911,
                  -0.386699020961393,
                   1.546796083845573,
                  -0.386699020961393,
                  -0.530330085889911,
                   0.005524271728020,
                   0.110485434560398,
                   0.027621358640100,
                   0.000000000000000
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 4/6).
        /// </summary>
        public static WaveletPack CDF46
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.088388347648318,
                   0.353553390593274,
                   0.530330085889911,
                   0.353553390593274,
                   0.088388347648318
                                   }, new double[] {
                  -0.006042172202522,
                  -0.024168688810087,
                   0.009494842032534,
                   0.158822812180572,
                   0.096156854765846,
                  -0.506161397079824,
                  -0.453162915189133,
                   1.450121328605225,
                  -0.453162915189133,
                  -0.506161397079824,
                   0.096156854765846,
                   0.158822812180572,
                   0.009494842032534,
                  -0.024168688810087,
                  -0.006042172202522,
                   0.000000000000000
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 6/2).
        /// </summary>
        public static WaveletPack CDF62
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.022097086912080,
                   0.132582521472478,
                   0.331456303681194,
                   0.441941738241592,
                   0.331456303681194,
                   0.132582521472478,
                   0.022097086912080 }, new double[] {
                   0.110485434560398,
                   0.662912607362388,
                   1.237436867076458,
                  -0.309359216769115,
                  -3.402951384460260,
                  -0.309359216769115,
                   1.237436867076458,
                   0.662912607362388,
                   0.110485434560398,
                   0.000000000000000
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 6/4).
        /// </summary>
        public static WaveletPack CDF64
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.022097086912080,
                   0.132582521472478,
                   0.331456303681194,
                   0.441941738241592,
                   0.331456303681194,
                   0.132582521472478,
                   0.022097086912080 }, new double[] {
                  -0.024168688810087,
                  -0.145012132860522,
                  -0.227876208780821,
                   0.324550964021169,
                   1.261605555886545,
                   0.174014559432627,
                  -2.726228097777822,
                   0.174014559432627,
                   1.261605555886545,
                   0.324550964021169,
                  -0.227876208780821,
                  -0.145012132860522,
                  -0.024168688810087,
                   0.000000000000000
                });
            }
        }
        /// <summary>
        /// Возвращает вейвлет Коэна-Добеши-Фево (CDF 6/6).
        /// </summary>
        public static WaveletPack CDF66
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                   0.022097086912080,
                   0.132582521472478,
                   0.331456303681194,
                   0.441941738241592,
                   0.331456303681194,
                   0.132582521472478,
                   0.022097086912080 }, new double[] {
                   0.005437954982270,
                   0.032627729893618,
                   0.041086770977148,
                  -0.134136222895983,
                  -0.380138948284370,
                   0.096156854765846,
                   1.196350096099310,
                   0.358905028829793,
                  -2.432578528735264,
                   0.358905028829793,
                   1.196350096099310,
                   0.096156854765846,
                  -0.380138948284370,
                  -0.134136222895983,
                   0.041086770977148,
                   0.032627729893618,
                   0.005437954982270,
                   0.000000000000000
                });
            }
        }
        /// <summary>
        /// Возвращает лифтинговый вейвлет Коэна-Добеши-Фево (CDF 9/7).
        /// </summary>
        public static WaveletPack CDF97
        {
            get
            {
                // Cohen–Daubechies–Feauveau wavelet:
                return WaveletPack.Create(new double[] {
                 3.782845550750114e-02,
                -2.384946501955685e-02,
                -1.106244044092826e-01,
                 3.774028556128305e-01,
                 8.526986789091245e-01,
                 3.774028557909638e-01,
                -1.106244045129673e-01,
                -2.384946502829822e-02,
                 3.782845552136610e-02 }, new double[] {
                 6.453888262876165e-02,
                -4.068941760920477e-02,
                -4.180922732220352e-01,
                 7.884856164063713e-01,
                -4.180922732220352e-01,
                -4.068941760920475e-02,
                 6.453888262876159e-02,
                 0.000000000000000e-00,
                 0.000000000000000e-00
                });
            }
        }
        #endregion

        #region Kravchenko wavelet
        /// <summary>
        /// Определяет вейвлет Кравченко.
        /// </summary>
        public static WaveletPack Kravchenko
        {
            get
            {
                // Left values of scale function:
                double[] left = new double[]
                {
                    0.438708321041,
                   -0.047099287129,
                   -0.118027008279,
                    0.037706980974,
                    0.043603935723,
                   -0.025214528289,
                   -0.011459893503,
                    0.013002207742,
                   -0.001878954975,
                   -0.003758906625,
                    0.005085949920,
                   -0.001349824585,
                   -0.003639380570,
                    0.002763059895,
                    0.001188712844,
                   -0.001940226446,
                    0.000384982816,
                    0.000499860951,
                   -0.000700388155,
                    0.000468702885,
                    0.000255769244,
                   -0.000649033581,
                    0.000266223602,
                    0.000307507863,
                   -0.000463771747,
                    0.000104807634,
                    0.000324973138,
                   -0.000288500372,
                   -0.000066833177,
                    0.000021430184,
                   -0.000018524173,
                   -0.000032851429,
                   -0.000000000000
                };

                // Right values of scale function:
                double[] right = new double[]
                {
                    0.757698251288,
                    0.438708321041,
                   -0.047099287129,
                   -0.118027008279,
                    0.037706980974,
                    0.043603935723,
                   -0.025214528289,
                   -0.011459893503,
                    0.013002207742,
                   -0.001878954975,
                   -0.003758906625,
                    0.005085949920,
                   -0.001349824585,
                   -0.003639380570,
                    0.002763059895,
                    0.001188712844,
                   -0.001940226446,
                    0.000384982816,
                    0.000499860951,
                   -0.000700388155,
                    0.000468702885,
                    0.000255769244,
                   -0.000649033581,
                    0.000266223602,
                    0.000307507863,
                   -0.000463771747,
                    0.000104807634,
                    0.000324973138,
                   -0.000288500372,
                   -0.000066833177,
                    0.000021430184,
                   -0.000018524173,
                   -0.000032851429
                };

                // Kravchenko orthogonal wavelet:
                return WaveletPack.Create(Matrice.Merge(left.Flip(), right));
            }
        }
        #endregion
    }
    #endregion

    #region Continuous wavelets
    /// <summary>
    /// Определяет непрерывный комплексный частотный B-сплайновый вейвлет.
    /// </summary>
    public class FbspWavelet : IComplexWavelet
    {
        #region Private data
        private double m;
        private double fb;
        private double fc;
        #endregion

        #region Wavelet compoents
        /// <summary>
        /// Инициализирует непрерывный комплексный частотный B-сплайновый вейвлет.
        /// </summary>
        /// <param name="m">Порядок вейвлета</param>
        /// <param name="fb">Параметр полосы пропускания</param>
        /// <param name="fc">Центральная частота вейвлета</param>
        public FbspWavelet(double m = 3, double fb = 1, double fc = 2)
        {
            M = m; Fb = fb; Fc = fc;
        }
        /// <summary>
        /// Получает или задает значение порядка вейвлета.
        /// </summary>
        public double M
        {
            get
            {
                return this.m;
            }
            set
            {
                if (value < 1)
                    throw new Exception("Неверное значение аргумента");

                this.m = value;
            }
        }
        /// <summary>
        /// Получает или задает значение параметра полосы пропускания.
        /// </summary>
        public double Fb
        {
            get
            {
                return this.fb;
            }
            set
            {
                this.fb = value;
            }
        }
        /// <summary>
        /// Получает или задает значение центральной частоты вейвлета.
        /// </summary>
        public double Fc
        {
            get
            {
                return this.fc;
            }
            set
            {
                this.fc = value;
            }
        }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Wavelet(double x)
        {
            double a = Math.Sqrt(fb);
            double b = x / Math.Pow(fb, m);
            double c = Special.Sinc(b, 1);
            double d = Math.Pow(c, m);
            Complex e = Maths.Exp(Maths.I * 2 * Maths.Pi * fc * x);
            return a * d * e;
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный вейвлет "Эрмитова шляпа".
    /// </summary>
    public class HermitianHatWavelet : IComplexWavelet
    {
        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный вейвлет "Эрмитова шляпа".
        /// </summary>
        public HermitianHatWavelet()
        {
        }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Wavelet(double x)
        {
            double x2 = x * x;
            double cf = 2.0 / Math.Sqrt(5) * Math.Pow(Math.PI, -0.25);
            return cf * (1 - x2 + Maths.I * x) * Maths.Exp(-0.5 * x2);
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный эрмитовый вейвлет.
    /// </summary>
    public class HermitianWavelet : IComplexWavelet
    {
        #region Private data
        private int derivative;
        #endregion

        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный эрмитовый вейвлет.
        /// </summary>
        /// <param name="derivative">Номер производной функции [1, 3]</param>
        public HermitianWavelet(int derivative = 1)
        {
            Derivative = derivative;
        }
        /// <summary>
        /// Получает или задает номер производной функции [1, 3].
        /// </summary>
        public int Derivative
        {
            get
            {
                return derivative;
            }
            set
            {
                if (value < 1 || value > 3)
                    throw new Exception("Неверное значение аргумента");

                derivative = value;
            }
        }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Wavelet(double x)
        {
            double x2 = x * x;
            Complex psi = 0;
            Complex f0 = Math.Pow(Math.PI, -0.25) * Maths.Exp(-x2 / 2);

            switch (derivative)
            {
                case 1:
                    psi = Math.Sqrt(2) * x * f0;
                    break;

                case 2:
                    psi = 2.0 * Math.Sqrt(3.0) / 3.0 * f0 * (1 - x2);
                    break;

                case 3:
                    psi = 2.0 * Math.Sqrt(30.0) / 15.0 * f0 * (x2 * x - 3 * x);
                    break;
            }

            return psi;
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный комплексный вейвлет Габора.
    /// </summary>
    public class GaborWavelet : IComplexWavelet
    {
        #region Private data
        private double x0;
        private double k0;
        private double a;
        private double a2;
        #endregion

        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный комплексный вейвлет Габора.
        /// </summary>
        /// <param name="x0">Начальное значение</param>
        /// <param name="k0">Коэффициент модуляции</param>
        /// <param name="a">Множитель</param>
        public GaborWavelet(double x0 = 0, double k0 = 1, double a = 2)
        {
            X0 = x0; K0 = k0; A = a;
        }
        /// <summary>
        /// Получает или задает начальное значение.
        /// </summary>
        public double X0
        {
            get
            {
                return this.x0;
            }
            set
            {
                this.x0 = value;
            }
        }
        /// <summary>
        /// Получает или задает коэффициент модуляции.
        /// </summary>
        public double K0
        {
            get
            {
                return this.k0;
            }
            set
            {
                this.k0 = value;
            }
        }
        /// <summary>
        /// Получает или задает множитель.
        /// </summary>
        public double A
        {
            get
            {
                return this.a;
            }
            set
            {
                this.a = value;
                this.a2 = a * a;
            }
        }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Wavelet(double x)
        {
            double d = x - x0;
            return Math.Exp(-d * d / a2) * Maths.Exp(-Maths.I * k0 * d);
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double WaveletReal(double x)
        {
            return Wavelet(x).Real;
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный комплексный Морлет вейвлет.
    /// </summary>
    public class ComplexMorletWavelet : IComplexWavelet
    {
        #region Private data
        private double fb;
        private double fc;
        #endregion

        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный комплексный Морлет вейвлет.
        /// </summary>
        /// <param name="fb">Полоса пропускания</param>
        /// <param name="fc">Центральная частота</param>
        public ComplexMorletWavelet(double fb = 0.5, double fc = 1)
        {
            Fb = fb; Fc = fc;
        }
        /// <summary>
        /// Получает или задает полосу пропускания.
        /// </summary>
        public double Fb
        {
            get
            {
                return this.fb;
            }
            set
            {
                this.fb = value;
            }
        }
        /// <summary>
        /// Получает или задает центральную частоту.
        /// </summary>
        public double Fc
        {
            get
            {
                return this.fc;
            }
            set
            {
                this.fc = value;
            }
        }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Wavelet(double x)
        {
            return Math.Pow(Maths.Pi * fb, -0.5) * Maths.Exp(2 * Maths.Pi * Maths.I * fc * x) * Math.Exp(-(x * x) / fb);
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный вейвлет Гаусса.
    /// </summary>
    public class ComplexGaussianWavelet : IComplexWavelet
    {
        #region Private data
        private int derivative;
        #endregion

        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный вейвлет Гаусса.
        /// </summary>
        /// <param name="derivative">Номер производной функции [1, 8]</param>
        public ComplexGaussianWavelet(int derivative = 1)
        {
            Derivative = derivative;
        }
        /// <summary>
        /// Получает или задает номер производной функции [1, 8].
        /// </summary>
        public int Derivative
        {
            get
            {
                return derivative;
            }
            set
            {
                if (value < 1 || value > 8)
                    throw new Exception("Неверное значение аргумента");

                derivative = value;
            }
        }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public Complex Wavelet(double x)
        {
            double x2 = x * x;
            Complex f0 = Maths.Exp(-x2);
            Complex f1 = Maths.Exp(-Maths.I * x);
            Complex f2 = (f1 * f0) / Math.Pow(Math.Exp(-1/2) * Math.Pow(2, 0.5) * Math.Pow(Maths.Pi, 0.5), 0.5);
            Complex psi = 0;

            // Gaussian wavelet ('):
            switch (derivative)
            {
                case 1:
                    psi = f2 * (-Maths.I - 2 * x) * Math.Pow(2, 0.5);
                    break;

                case 2:
                    psi = 1.0 / 3 * f2 * (-3 + 4 * Maths.I * x + 4 * x2) * Math.Pow(6, 0.5);
                    break;

                case 3:
                    psi = 1.0 / 15 * f2 * (7 * Maths.I + 18 * x - 12 * Maths.I * x * x - 8 * Math.Pow(x, 3)) * Math.Pow(30, 0.5);
                    break;

                case 4:
                    psi = 1.0 / 105 * f2 * (25 - 56 * Maths.I * x - 72 * x * x + 32 * Maths.I * Math.Pow(x, 3) + 16 * Math.Pow(x, 4)) * Math.Pow(210, 0.5);
                    break;

                case 5:
                    psi = 1.0 / 315 * f2 * (-81 * Maths.I - 250 * x +280 * Maths.I * x * x + 240 * Maths.Pow(x, 3) - 80 * Maths.I * Math.Pow(x, 4) - 32 * Math.Pow(x, 5)) * Math.Pow(210, 0.5);
                    break;

                case 6:
                    psi = 1.0 / 3465 * f2 * (-331 + 972 * Maths.I * x + 1500 * x * x - 1120 * Maths.I * Math.Pow(x, 3) - 720 * Math.Pow(x, 4) + 192 * Maths.I* Math.Pow(x, 5) + 64 * Math.Pow(x, 6)) * Math.Pow(2310, 0.5);
                    break;

                case 7:
                    psi = 1.0 / 45045 * f2 * (1303 * Maths.I + 4634 * x - 6804 * Maths.I * x * x - 7000 * Math.Pow(x, 3) + 3920 * Maths.I * Math.Pow(x, 4) + 2016 * Math.Pow(x, 5) - 448 * Maths.I * Math.Pow(x, 6) - 128 * Math.Pow(x, 7)) * Maths.Pow(30030, 0.5);
                    break;

                case 8:
                    psi = 1.0 / 45045 * f2 * (5937 - 20848 * Maths.I * x - 37072 * x * x +36288 * Maths.I * Math.Pow(x, 3) + 28000 * Math.Pow(x, 4) - 12544 * Maths.I * Math.Pow(x, 5) - 5376 * Math.Pow(x, 6) + 1024 * Maths.I * Math.Pow(x, 7) + 256 * Math.Pow(x, 8)) * Math.Pow(20021, 0.5);
                    break;
            }

            return psi;
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный вейвлет Гаусса.
    /// </summary>
    public class GaussianWavelet : IDoubleWavelet
    {
        #region Private data
        private int derivative;
        #endregion

        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный вейвлет Гаусса.
        /// </summary>
        /// <param name="derivative">Номер производной функции [1, 8]</param>
        public GaussianWavelet(int derivative = 1)
        {
            Derivative = derivative;
        }
        /// <summary>
        /// Получает или задает номер производной функции [1, 8].
        /// </summary>
        public int Derivative
        {
            get
            {
                return derivative;
            }
            set
            {
                if (value < 1 || value > 8)
                    throw new Exception("Неверное значение аргумента");

                derivative = value;
            }
        }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Wavelet(double x)
        {
            double x2 = x * x;
            double f0 = Math.Pow(2.0 / Math.PI, 0.25) * Math.Exp(-x2);
            double psi = 0;

            // Gaussian wavelet ('):
            switch (derivative)
            {
                case 1:
                    psi = -2.0 * x * f0;
                    break;

                case 2:
                    psi = 2.0 / Math.Pow(3, 0.5) * (-1.0 + 2 * x2) * f0;
                    break;

                case 3:
                    psi = 4.0 / Math.Pow(15, 0.5) * x * (3 - 2 * x2) * f0;
                    break;

                case 4:
                    psi = 4.0 / Math.Pow(105, 0.5) * (3 - 12 * x2 + 4 * x2 * x2) * f0;
                    break;

                case 5:
                    psi = 8.0 / (3 * Math.Pow(105, 0.5)) * x * (-15 + 20 * x2 - 4 * x2 * x2) * f0;
                    break;

                case 6:
                    psi = 8.0 / (3 * Math.Pow(1155, 0.5)) * (-15 + 90 * x2 - 60 * x2 * x2 + 8 * Math.Pow(x2, 3)) * f0;
                    break;

                case 7:
                    psi = 16.0 / (3 * Math.Pow(15015, 0.5)) * x * (105 - 210 * x2 + 84 * x2 * x2 - 8 * Math.Pow(x2, 3)) * f0;
                    break;

                case 8:
                    psi = 16.0 / (45 * Math.Pow(1001, 0.5)) * (105 - 840 * x2 + 840 * x2 * x2 - 224 * Math.Pow(x2, 3) + 16 * Math.Pow(x2, 4)) * f0;
                    break;
            }

            return psi;
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный вейвлет "Сомбреро".
    /// </summary>
    public class MexicanHatWavelet : IDoubleWavelet
    {
        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный вейвлет "Сомбреро".
        /// </summary>
        public MexicanHatWavelet()
        {
        }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Wavelet(double x)
        {
            double x2 = x * x;
            return 2.0 / (Math.Sqrt(3) * Math.Pow(Math.PI, 0.25)) * (1 - x2) * Math.Exp(-x2 / 2);
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный Морлет вейвлет.
    /// </summary>
    public class MorletWavelet : IDoubleWavelet
    {
        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный Морлет вейвлет.
        /// </summary>
        public MorletWavelet()
        {
        }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Wavelet(double x)
        {
            double x2 = x * x;
            return Math.Exp(-x2 / 2) * Math.Cos(5 * x);
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный вейвлет Мейера.
    /// </summary>
    public class MeyerWavelet : IDoubleWavelet
    {
        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный вейвлет Мейера.
        /// </summary>
        public MeyerWavelet() { }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Scaling(double x)
        {
            // 2015, Victor Vermehren Valenzuela and H. M. de Oliveira gave 
            // the explicit expressions of Meyer wavelet and scale functions:
            if (x == 0)
            {
                return 2.0 / 3 + 4.0 / (3 * Math.PI);
            }
            double phiupper = Math.Sin(2.0 * Math.PI / 3 * x) + 4.0 / 3 * x * Math.Cos(4 * Math.PI / 3 * x);
            double phidown = Math.PI * x - 16 * Math.PI / 9 * Math.Pow(x, 3);
            return phiupper / phidown;
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Wavelet(double x)
        {
            // 2015, Victor Vermehren Valenzuela and H. M. de Oliveira gave 
            // the explicit expressions of Meyer wavelet and scale functions:
            //
            // Kernel value:
            double t = x - 0.5;
            // Finding ψ1(t):
            double psi1upper = 4.0 / (3 * Math.PI) * t * Math.Cos(2 * Math.PI / 3 * t) - 1.0 / Math.PI * Math.Sin(4 * Math.PI / 3 * t);
            double psi1down = t - 16.0 / 9 * Math.Pow(t, 3);
            // Finding ψ2(t):
            double psi2upper = 8.0 / (3 * Math.PI) * t * Math.Cos(8 * Math.PI / 3 * t) + 1.0 / Math.PI * Math.Sin(4 * Math.PI / 3 * t);
            double psi2down = t - 64.0 / 9 * Math.Pow(t, 3);
            // Finding ψ(t) = ψ1(t) + ψ2(t):
            return psi1upper / psi1down + psi2upper / psi2down;
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный вейвлет Шеннона.
    /// </summary>
    public class ShannonWavelet : IDoubleWavelet
    {
        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный вейвлет Шеннона.
        /// </summary>
        public ShannonWavelet() { }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Wavelet(double x)
        {
            double t = x / 2;
            return Special.Sinc(t) * Math.Cos(3 * Math.PI * t);
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный вейвлет Пуассона.
    /// </summary>
    public class PoissonWavelet : IDoubleWavelet
    {
        #region Private data
        private int n;
        #endregion

        #region Wavelet components
        /// <summary>
        /// Инициализирует непрерывный вейвлет Пуассона.
        /// </summary>
        /// <param name="n">Порядок вейвлета [1, +inf)</param>
        public PoissonWavelet(int n = 1)
        {
            N = n;
        }
        /// <summary>
        /// Получает или задает порядок вейвлета [1, +inf).
        /// </summary>
        public int N
        {
            get
            {
                return n;
            }
            set
            {
                if (value < 1)
                    throw new Exception("Неверное значение аргумента");

                n = value;
            }
        }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Scaling(double x)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Wavelet(double x)
        {
            if (x < 0)
            {
                return 0;
            }
            return (x - n) / Special.Factorial(n) * Math.Pow(x, n - 1) * Math.Exp(-x);
        }
        #endregion
    }
    /// <summary>
    /// Определяет непрерывный вейвлет Хаара.
    /// </summary>
    public class HaarWavelet : IDoubleWavelet
    {
        #region Haar wavelet components
        /// <summary>
        /// Инициализирует непрерывный вейвлет Хаара.
        /// </summary>
        public HaarWavelet() { }
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Scaling(double x)
        {
            if (0 <= x && x < 1)
            {
                return 1.0;
            }
            return 0.0;
        }
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        public double Wavelet(double x)
        {
            if (x >= 0)
            {
                return (x < 0.5) ? 1.0 : -1.0;
            }
            return 0.0;
        }
        #endregion
    }
    #endregion

    #region Processing filters
    /// <summary>
    /// Определяет вейвлет-фильтр.
    /// </summary>
    public class WaveletFilter : IFilter, IBlendFilter
    {
        #region Private data
        WaveletTransform dwt;
        double factor = -1.0;
        double accuracy = 0.1;
        #endregion

        #region Filter components
        /// <summary>
        /// Инициализирует вейвлет-фильтр.
        /// </summary>
        /// <param name="dwt">Дискретное вейвлет-преобразование</param>
        /// <param name="factor">Множитель [-1, 1]</param>
        /// <param name="accuracy">Точность фильтра [0, 1]</param>
        public WaveletFilter(WaveletTransform dwt, double factor = -1.0, double accuracy = 0.1)
        {
            this.dwt = dwt;
            this.factor = factor;
            this.Accuracy = accuracy;
        }
        /// <summary>
        /// Получает или задает дискретное вейвлет-преобразование.
        /// </summary>
        public WaveletTransform DWT
        {
            get
            {
                return this.dwt;
            }
            set
            {
                this.dwt = value;
            }
        }
        /// <summary>
        /// Получает или задает значение точности фильтра [0, 1].
        /// </summary>
        public double Accuracy
        {
            get
            {
                return this.accuracy;
            }
            set
            {
                this.accuracy = Maths.Double(value);
            }
        }
        /// <summary>
        /// Получает или задает значение множителя [-1, 1].
        /// </summary>
        public double Factor
        {
            get
            {
                return this.factor;
            }
            set
            {
                this.factor = value;
            }
        }
        #endregion

        #region Public apply voids
        /// <summary>
        /// Реализует двумерный вейвлет-фильтр.
        /// </summary>
        /// <param name="data">Матрица</param>

        public void Apply(double[,] data)
        {
            // extend input
            int r0 = data.GetLength(0), c0 = data.GetLength(1);
            int delta = (int)(Math.Min(r0, c0) * accuracy);
            int r = WaveletFilter.GetLength(r0 + delta * 2, dwt.Levels);
            int c = WaveletFilter.GetLength(c0 + delta * 2, dwt.Levels);
            double[,] extd = Matrice.Extend(data, r, c);

            // wavelets
            double[,] wave;
            double alfa = 1 + factor;
            int maxl = WaveletFilter.GetMaxLevels(Math.Min(r, c), dwt.Levels);
            int powR = r >> maxl;
            int powC = c >> maxl;
            int j, k;

            // forward wavelet transform
            wave = dwt.Forward(extd);

            // do job
            for (j = 0; j < r; j++)
            {
                for (k = 0; k < c; k++)
                {
                    if (j < powR && k < powC)
                        // low-pass
                        extd[j, k] = wave[j, k];
                    else
                        // high-pass filter
                        extd[j, k] = wave[j, k] * alfa;
                }
            }

            // backward wavelet transform
            extd = dwt.Backward(extd);

            // cutend result
            int y0 = (r - r0) / 2;
            int x0 = (c - c0) / 2;
            extd = Matrice.Cut(extd, y0, x0, r0, c0);

            for (j = 0; j < r0; j++)
                for (k = 0; k < c0; k++)
                    data[j, k] = extd[j, k];

            return;
        }
        /// <summary>
        /// Реализует одномерный вейвлет-фильтр.
        /// </summary>
        /// <param name="data">Одномерный массив</param>
        public void Apply(double[] data)
        {
            // params
            int r0 = data.GetLength(0);
            int delta = (int)(r0 * accuracy);
            int r = WaveletFilter.GetLength(r0 + delta * 2, dwt.Levels);
            double[] extd = Matrice.Extend(data, r);

            // wavelets
            double[] wave;
            double alfa = 1 + factor;
            int maxl = WaveletFilter.GetMaxLevels(r, dwt.Levels);
            int powR = r >> maxl;
            int j;

            // forward wavelet transform
            wave = dwt.Forward(extd);

            // do job
            for (j = 0; j < r; j++)
            {
                if (j < powR)
                    // low-pass
                    extd[j] = wave[j];
                else
                    // high-pass filter
                    extd[j] = wave[j] * alfa;
            }

            // backward wavelet transform
            extd = dwt.Backward(extd);

            // cutend result
            int y0 = (r - r0) / 2;
            extd = Matrice.Cut(extd, y0, r0);

            for (j = 0; j < r0; j++)
                data[j] = extd[j];

            return;
        }
        /// <summary>
        /// Реализует двумерный вейвлет-фильтр.
        /// </summary>
        /// <param name="data">Матрица</param>

        public void Apply(Complex[,] data)
        {
            // extend input
            int r0 = data.GetLength(0), c0 = data.GetLength(1);
            int delta = (int)(Math.Min(r0, c0) * accuracy);
            int r = WaveletFilter.GetLength(r0 + delta * 2, dwt.Levels);
            int c = WaveletFilter.GetLength(c0 + delta * 2, dwt.Levels);
            Complex[,] extd = Matrice.Extend(data, r, c);

            // wavelets
            Complex[,] wave;
            double alfa = 1 + factor;
            int maxl = WaveletFilter.GetMaxLevels(Math.Min(r, c), dwt.Levels);
            int powR = r >> maxl;
            int powC = c >> maxl;
            int j, k;

            // forward wavelet transform
            wave = dwt.Forward(extd);

            // do job
            for (j = 0; j < r; j++)
            {
                for (k = 0; k < c; k++)
                {
                    if (j < powR && k < powC)
                        // low-pass
                        extd[j, k] = wave[j, k];
                    else
                        // high-pass filter
                        extd[j, k] = wave[j, k] * alfa;
                }
            }

            // backward wavelet transform
            extd = dwt.Backward(extd);

            // cutend result
            int y0 = (r - r0) / 2;
            int x0 = (c - c0) / 2;
            extd = Matrice.Cut(extd, y0, x0, r0, c0);

            for (j = 0; j < r0; j++)
                for (k = 0; k < c0; k++)
                    data[j, k] = extd[j, k];

            return;
        }
        /// <summary>
        /// Реализует одномерный вейвлет-фильтр.
        /// </summary>
        /// <param name="data">Одномерный массив</param>
        public void Apply(Complex[] data)
        {
            // params
            int r0 = data.GetLength(0);
            int delta = (int)(r0 * accuracy);
            int r = WaveletFilter.GetLength(r0 + delta * 2, dwt.Levels);
            Complex[] extd = Matrice.Extend(data, r);

            // wavelets
            Complex[] wave;
            double alfa = 1 + factor;
            int maxl = WaveletFilter.GetMaxLevels(r, dwt.Levels);
            int powR = r >> maxl;
            int j;

            // forward wavelet transform
            wave = dwt.Forward(extd);

            // do job
            for (j = 0; j < r; j++)
            {
                if (j < powR)
                    // low-pass
                    extd[j] = wave[j];
                else
                    // high-pass filter
                    extd[j] = wave[j] * alfa;
            }

            // backward wavelet transform
            extd = dwt.Backward(extd);

            // cutend result
            int y0 = (r - r0) / 2;
            extd = Matrice.Cut(extd, y0, r0);

            for (j = 0; j < r0; j++)
                data[j] = extd[j];

            return;
        }
        #endregion

        #region Blender apply voids
        /// <summary>
        /// Реализует двумерный вейвлет-фильтр.
        /// </summary>
        /// <param name="data">Набор матриц</param>
        /// <returns>Матрица</returns>

        public double[,] Apply(double[][,] data)
        {
            // params
            int length = data.Length;
            double[,] extd = data[0];
            int r0 = extd.GetLength(0), c0 = extd.GetLength(1);

            // extend input
            int delta = (int)(Math.Min(r0, c0) * accuracy);
            int r = WaveletFilter.GetLength(r0 + delta * 2, dwt.Levels);
            int c = WaveletFilter.GetLength(c0 + delta * 2, dwt.Levels);
            double[,] sum = new double[r, c];

            // wavelets
            double[,] wave;
            double alfa = 1 + factor;
            int maxl = WaveletFilter.GetMaxLevels(Math.Min(r, c), dwt.Levels);
            int powR = r >> maxl;
            int powC = c >> maxl;
            int j, k;

            // do job
            for (int i = 0; i < length; i++)
            {
                // forward wavelet transform
                extd = Matrice.Extend(data[i], r, c);
                wave = dwt.Forward(extd);

                for (j = 0; j < r; j++)
                {
                    for (k = 0; k < c; k++)
                    {
                        if (j < powR && k < powC)
                            // low-pass
                            sum[j, k] += wave[j, k] / length;
                        else
                            // high-pass filter
                            sum[j, k] += wave[j, k] * alfa / length;
                    }
                }
            }

            // backward wavelet transform
            sum = dwt.Backward(sum);

            // cutend result
            int y0 = (r - r0) / 2;
            int x0 = (c - c0) / 2;
            return Matrice.Cut(sum, y0, x0, r0, c0);
        }
        /// <summary>
        /// Реализует двумерный вейвлет-фильтр.
        /// </summary>
        /// <param name="data">Набор матриц</param>
        /// <returns>Матрица</returns>

        public double[] Apply(double[][] data)
        {
            // params
            int length = data.Length;
            double[] extd = data[0];
            int r0 = extd.GetLength(0);

            // extend input
            int delta = (int)(r0 * accuracy);
            int r = GetLength(r0 + delta * 2, dwt.Levels);
            double[] sum = new double[r];

            // wavelets
            double[] wave;
            int maxl = WaveletFilter.GetMaxLevels(r, dwt.Levels);
            double alfa = 1 + factor;
            int powR = r >> maxl;
            int j;

            // do job
            for (int i = 0; i < length; i++)
            {
                // forward wavelet transform
                extd = Matrice.Extend(data[i], r);
                wave = dwt.Forward(extd);

                for (j = 0; j < r; j++)
                {
                    if (j < powR)
                        // low-pass
                        sum[j] += wave[j] / length;
                    else
                        // high-pass filter
                        sum[j] += wave[j] * alfa / length;
                }
            }

            // backward wavelet transform
            sum = dwt.Backward(sum);

            // cutend result
            int y0 = (r - r0) / 2;
            return Matrice.Cut(sum, y0, r0);
        }
        /// <summary>
        /// Реализует двумерный вейвлет-фильтр.
        /// </summary>
        /// <param name="data">Набор матриц</param>
        /// <returns>Матрица</returns>

        public Complex[,] Apply(Complex[][,] data)
        {
            // params
            int length = data.Length;
            Complex[,] extd = data[0];
            int r0 = extd.GetLength(0), c0 = extd.GetLength(1);

            // extend input
            int delta = (int)(Math.Min(r0, c0) * accuracy);
            int r = WaveletFilter.GetLength(r0 + delta * 2, dwt.Levels);
            int c = WaveletFilter.GetLength(c0 + delta * 2, dwt.Levels);
            Complex[,] sum = new Complex[r, c];

            // wavelets
            Complex[,] wave;
            double alfa = 1 + factor;
            int maxl = WaveletFilter.GetMaxLevels(Math.Min(r, c), dwt.Levels);
            int powR = r >> maxl;
            int powC = c >> maxl;
            int j, k;

            // do job
            for (int i = 0; i < length; i++)
            {
                // forward wavelet transform
                extd = Matrice.Extend(data[i], r, c);
                wave = dwt.Forward(extd);

                for (j = 0; j < r; j++)
                {
                    for (k = 0; k < c; k++)
                    {
                        if (j < powR && k < powC)
                            // low-pass
                            sum[j, k] += wave[j, k] / length;
                        else
                            // high-pass filter
                            sum[j, k] += wave[j, k] * alfa / length;
                    }
                }
            }

            // backward wavelet transform
            sum = dwt.Backward(sum);

            // cutend result
            int y0 = (r - r0) / 2;
            int x0 = (c - c0) / 2;
            return Matrice.Cut(sum, y0, x0, r0, c0);
        }
        /// <summary>
        /// Реализует двумерный вейвлет-фильтр.
        /// </summary>
        /// <param name="data">Набор матриц</param>
        /// <returns>Матрица</returns>

        public Complex[] Apply(Complex[][] data)
        {
            // params
            int length = data.Length;
            Complex[] extd = data[0];
            int r0 = extd.GetLength(0);

            // extend input
            int delta = (int)(r0 * accuracy);
            int r = GetLength(r0 + delta * 2, dwt.Levels);
            Complex[] sum = new Complex[r];

            // wavelets
            Complex[] wave;
            int maxl = WaveletFilter.GetMaxLevels(r, dwt.Levels);
            double alfa = 1 + factor;
            int powR = r >> maxl;
            int j;

            // do job
            for (int i = 0; i < length; i++)
            {
                // forward wavelet transform
                extd = Matrice.Extend(data[i], r);
                wave = dwt.Forward(extd);

                for (j = 0; j < r; j++)
                {
                    if (j < powR)
                        // low-pass
                        sum[j] += wave[j] / length;
                    else
                        // high-pass filter
                        sum[j] += wave[j] * alfa / length;
                }
            }

            // backward wavelet transform
            sum = dwt.Backward(sum);

            // cutend result
            int y0 = (r - r0) / 2;
            return Matrice.Cut(sum, y0, r0);
        }
        #endregion

        #region Static voids
        /// <summary>
        /// Возвращает значение длины для преобразования.
        /// </summary>
        /// <param name="n">Длина</param>
        /// <param name="levels">Количество уровней</param>
        /// <returns>Длина</returns>
        public static int GetLength(int n, int levels)
        {
            // params
            int log2 = GetMaxLevels(n, levels);
            int s = n, m, i;

            // do job
            for (i = 0; i < log2; i++)
            {
                m = Maths.Mod(s, 2);

                if (s >= 2)
                {
                    if (m != 0)
                        s = s + 1;
                    s = s / 2;
                }
            }

            return s * (int)Math.Pow(2, i);
        }
        /// <summary>
        /// Returns max levels of 2^K transform.
        /// </summary>
        /// <param name="n">Length</param>
        /// <param name="levels">Levels</param>
        /// <returns>New length</returns>
        private static int GetMaxLevels(int n, int levels)
        {
            return (int)Math.Min(Math.Log(n, 2), levels);
        }
        #endregion
    }
    #endregion

    #region Wavelet interfaces
    /// <summary>
    /// Определяет общий интерфейс непрерывных комплексных вейвлетов.
    /// </summary>
    public interface IComplexWavelet
    {
        #region Interface
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        Complex Scaling(double x);
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        Complex Wavelet(double x);
        #endregion
    }
    /// <summary>
    /// Определяет общий интерфейс непрерывных вещественных вейвлетов.
    /// </summary>
    public interface IDoubleWavelet
    {
        #region Interface
        /// <summary>
        /// Возвращает значение масштабирующей функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        double Scaling(double x);
        /// <summary>
        /// Возвращает значение вейвлет-функции.
        /// </summary>
        /// <param name="x">Носитель</param>
        /// <returns>Значение функции</returns>
        double Wavelet(double x);
        #endregion
    }
    /// <summary>
    /// Определяет общий интерфейс вейвлет-преобразований.
    /// </summary>
    public interface IWaveletTransform
    {
        #region Interface
        /// <summary>
        /// Получает или задает дискретный вейвлет.
        /// </summary>
        WaveletPack Wavelet { get; set; }
        #endregion
    }
    #endregion
}
