using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Services.Common
{
    public sealed class CompositeProgress : IProgress<float>
    {
        private readonly Action<float> _handler;
        private readonly Func<Action<float>, IProgress<float>> _subProgressFactory;

        private readonly List<float> _subProgressValues;
        private readonly List<float> _subProgressWeights;

        private float _subValuesSum;
        private float _subWeightsSum;
        private bool _updateEnabled;

        public CompositeProgress(
            Action<float> handler,
            bool autoEnableUpdate = true,
            Func<Action<float>, IProgress<float>> subProgressFactory = null)
        {
            _handler = handler;
            _updateEnabled = autoEnableUpdate;

            _subProgressFactory = subProgressFactory ?? (h => Progress.Create(h));

            _subProgressValues = new List<float>();
            _subProgressWeights = new List<float>();
        }

        /// <summary>
        /// Do not use. Should be updated using only sub progresses.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotSupportedException"></exception>
        void IProgress<float>.Report(float value)
        {
            throw new NotSupportedException(
                $"{nameof(CompositeProgress)} aggregates only sub-progresses created via {nameof(CreateSubProgress)}.");
        }

        public IProgress<float> CreateSubProgress(float weight = 1)
        {
            weight = weight > 0f ? weight : 0f;

            int subIndex = _subProgressValues.Count;

            _subProgressValues.Add(0f);
            _subProgressWeights.Add(weight);

            _subWeightsSum += weight;

            return _subProgressFactory(subValue => RecalculateTotalProgress(subIndex, subValue));
        }

        public void EnableUpdate()
        {
            _updateEnabled = true;
            _handler(_subWeightsSum > 0 ? _subValuesSum / _subWeightsSum : 0f);
        }

        private void RecalculateTotalProgress(int subIndex, float subValue)
        {
            subValue = Mathf.Clamp01(subValue);

            _subValuesSum += (subValue - _subProgressValues[subIndex]) * _subProgressWeights[subIndex];
            _subProgressValues[subIndex] = subValue;

            if (!_updateEnabled)
                return;

            _handler(_subWeightsSum > 0 ? _subValuesSum / _subWeightsSum : 0f);
        }
    }
}