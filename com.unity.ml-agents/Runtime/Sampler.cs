using System;
using System.Linq;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Inference.Utils;
using UnityEngine;

namespace Unity.MLAgents
{
    /// <summary>
    /// The types of distributions from which to sample reset parameters.
    /// </summary>
    internal enum SamplerType
    {
        /// <summary>
        /// Samples a reset parameter from a uniform distribution.
        /// </summary>
        Uniform = 0,

        /// <summary>
        /// Samples a reset parameter from a Gaussian distribution.
        /// </summary>
        Gaussian = 1,

        /// <summary>
        /// Samples a reset parameter from a Gaussian distribution.
        /// </summary>
        MultiRangeUniform = 2

    }

    /// <summary>
    /// Takes a list of floats that encode a sampling distribution and returns the sampling function.
    /// </summary>
    internal sealed class SamplerFactory
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal SamplerFactory()
        {
        }

        /// <summary>
        /// Create the sampling distribution described by the encoding.
        /// </summary>
        /// <param name="encoding"> List of floats the describe sampling destribution.</param>
        public Func<float> CreateSampler(IList<float> encoding, int seed)
        {
            if ((int)encoding[0] == (int)SamplerType.Uniform)
            {
                return CreateUniformSampler(encoding[1], encoding[2], seed);
            }
            else if ((int)encoding[0] == (int)SamplerType.Gaussian)
            {
                return CreateGaussianSampler(encoding[1], encoding[2], seed);
            }
            else if ((int)encoding[0] == (int)SamplerType.MultiRangeUniform)
            {
                return CreateMultiRangeUniformSampler(encoding, seed);
            }
            else{
                Debug.LogWarning("EnvironmentParametersChannel received an unknown data type.");
                return () => 0;
            }

        }

        internal Func<float> CreateUniformSampler(float min, float max, int seed)
        {
            System.Random distr = new System.Random(seed);
            return () => min + (float)distr.NextDouble() * (max - min);
        }

        internal Func<float> CreateGaussianSampler(float mean, float stddev, int seed)
        {
            RandomNormal distr = new RandomNormal(seed, mean, stddev);
            return () => (float)distr.NextDouble();
        }

        internal Func<float> CreateMultiRangeUniformSampler(IList<float> encoding, int seed)
        {
            //RNG
            System.Random distr = new System.Random(seed);
            // Skip type of distribution since already checked to get into this function
            var sampler_encoding = encoding.Skip(1);
            // Will be used to normalize intervals
            float sum_interval_sizes = 0;
            //The number of intervals
            int num_intervals = (int)(sampler_encoding.Count()/2);
            // List that will store interval lengths
            float[] interval_sizes = new float[num_intervals];
            // List that will store uniform distributions
            IList<Func<float>> intervals = new Func<float>[num_intervals];
            // Collect all intervals and store as uniform distrus
            // Collect all interval sizes
            for(int i = 0; i < num_intervals; i++)
            {
                var min = sampler_encoding.ElementAt(2 * i);
                var max = sampler_encoding.ElementAt(2 * i + 1);
                var interval_size = max - min;
                sum_interval_sizes += interval_size;
                interval_sizes[i] = interval_size;
                intervals[i] = () => min + (float)distr.NextDouble() * interval_size;
            }
            // Normalize interval lengths
            for(int i = 0; i < num_intervals; i++)
            {
                interval_sizes[i] = interval_sizes[i] / sum_interval_sizes;
            } 
            // Build cmf for intervals
            for(int i = 1; i < num_intervals; i++)
            {
                interval_sizes[i] += interval_sizes[i - 1];
            } 
            Multinomial intervalDistr = new Multinomial(seed);
            float MultiRange()
            {
                int sampledInterval = intervalDistr.Sample(interval_sizes);
                return intervals[sampledInterval].Invoke();
            }
            return MultiRange;
        }
    }
}