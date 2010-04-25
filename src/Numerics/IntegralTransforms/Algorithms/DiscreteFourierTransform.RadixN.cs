﻿// <copyright file="DiscreteFourierTransform.RadixN.cs" company="Math.NET">
// Math.NET Numerics, part of the Math.NET Project
// http://mathnet.opensourcedotnet.info
//
// Copyright (c) 2009 Math.NET
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
// </copyright>

namespace MathNet.Numerics.IntegralTransforms.Algorithms
{
    using System;
    using System.Numerics;
    using NumberTheory;
    using Properties;
    using Threading;

    /// <summary>
    /// Complex Fast (FFT) Implementation of the Discrete Fourier Transform (DFT).
    /// </summary>
    public partial class DiscreteFourierTransform
    {
        /// <summary>
        /// Radix-2 Reorder Helper Method
        /// </summary>
        /// <typeparam name="T">Sample type</typeparam>
        /// <param name="samples">Sample vector</param>
        private static void Radix2Reorder<T>(T[] samples)
        {
            int j = 0;
            for (int i = 0; i < samples.Length - 1; i++)
            {
                if (i < j)
                {
                    T temp = samples[i];
                    samples[i] = samples[j];
                    samples[j] = temp;
                }

                int m = samples.Length;

                do
                {
                    m >>= 1;
                    j ^= m;
                }
                while ((j & m) == 0);
            }
        }

        /// <summary>
        /// Radix-2 Step Helper Method
        /// </summary>
        /// <param name="samples">Sample vector.</param>
        /// <param name="exponentSign">Fourier series exponent sign.</param>
        /// <param name="levelSize">Level Group Size.</param>
        /// <param name="k">Index inside of the level.</param>
        private static void Radix2Step(Complex[] samples, int exponentSign, int levelSize, int k)
        {
            // Twiddle Factor
            double exponent = (exponentSign * k) * Constants.Pi / levelSize;
            Complex w = new Complex(Math.Cos(exponent), Math.Sin(exponent));

            int step = levelSize << 1;
            for (int i = k; i < samples.Length; i += step)
            {
                Complex ai = samples[i];
                Complex t = w * samples[i + levelSize];
                samples[i] = ai + t;
                samples[i + levelSize] = ai - t;
            }
        }

        /// <summary>
        /// Radix-2 generic FFT for power-of-two sized sample vectors.
        /// </summary>
        /// <param name="samples">Sample vector, where the FFT is evaluated in place.</param>
        /// <param name="exponentSign">Fourier series exponent sign.</param>
        /// <exception cref="ArgumentException"/>
        internal static void Radix2(Complex[] samples, int exponentSign)
        {
            if (!samples.Length.IsPowerOfTwo())
            {
                throw new ArgumentException(Resources.ArgumentPowerOfTwo);
            }

            Radix2Reorder(samples);
            for (int levelSize = 1; levelSize < samples.Length; levelSize *= 2)
            {
                for (int k = 0; k < levelSize; k++)
                {
                    Radix2Step(samples, exponentSign, levelSize, k);
                }
            }
        }

        /// <summary>
        /// Radix-2 generic FFT for power-of-two sample vectors (Parallel Version).
        /// </summary>
        /// <param name="samples">Sample vector, where the FFT is evaluated in place.</param>
        /// <param name="exponentSign">Fourier series exponent sign.</param>
        /// <exception cref="ArgumentException"/>
        internal static void Radix2Parallel(Complex[] samples, int exponentSign)
        {
            if (!samples.Length.IsPowerOfTwo())
            {
                throw new ArgumentException(Resources.ArgumentPowerOfTwo);
            }

            Radix2Reorder(samples);
            for (int levelSize = 1; levelSize < samples.Length; levelSize *= 2)
            {
                int size = levelSize;
                Parallel.For(
                    0,
                    size,
                    k => Radix2Step(samples, exponentSign, size, k));
            }
        }

        /// <summary>
        /// Radix-2 forward FFT for power-of-two sized sample vectors.
        /// </summary>
        /// <param name="samples">Sample vector, where the FFT is evaluated in place.</param>
        /// <param name="options">Fourier Transform Convention Options.</param>
        /// <exception cref="ArgumentException"/>
        public void Radix2Forward(Complex[] samples, FourierOptions options)
        {
            Radix2Parallel(samples, SignByOptions(options));
            ForwardScaleByOptions(options, samples);
        }

        /// <summary>
        /// Radix-2 inverse FFT for power-of-two sized sample vectors.
        /// </summary>
        /// <param name="samples">Sample vector, where the FFT is evaluated in place.</param>
        /// <param name="options">Fourier Transform Convention Options.</param>
        /// <exception cref="ArgumentException"/>
        public void Radix2Inverse(Complex[] samples, FourierOptions options)
        {
            Radix2Parallel(samples, -SignByOptions(options));
            InverseScaleByOptions(options, samples);
        }
    }
}
