using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.WasapiAudio.Scripts.Core;
using Assets.WasapiAudio.Scripts.Wasapi;

namespace Assets.WasapiAudio.Scripts.Unity
{


    public class WasapiAudioPeer : AudioVisualizationEffect
    {
        WasapiAudioSource _wasapiAudioSource;
       // AudioSource _audioSource;

        public int debugband;

        public static float[] _samples = new float[512];
        public  float[] _samplesdebug = new float[512];
        public static float[] _freqBand = new float[8];
        public static float[] _bandBuffer = new float[8];
        public static float[] _bufferDecrease = new float[8];


        float[] _freqBandHighest = new float[8];
        public static float[] _audioBand = new float[8];
        public static float[] _audioBandBuffer = new float[8];

        public static float _amplitude, _amplitudeBuffer;

        public float _micAmplitude;

        float _amplitudeHighest;
        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            GetSpectrumAudioSource();
            MakeFrequencyBands();
            BandBuffer();
            CreateAudioBands();
            GetAmplitude();

            Debug.Log(_audioBand[debugband]);
            _samplesdebug = _samples;
        }


        void GetAmplitude()
        {
            float _currentAmplitude = 0;
            float _currentAmplitudeBuffer = 0;
            for (int i = 0; i < 8; i++)
            {
                _currentAmplitude += _audioBand[i];
                _currentAmplitudeBuffer += _audioBandBuffer[i];
            }

            if (_currentAmplitude > _amplitudeHighest)
            {
                _amplitudeHighest = _currentAmplitude;
            }

            _amplitude = _currentAmplitude / _amplitudeHighest;
            _amplitudeBuffer = _currentAmplitudeBuffer / _amplitudeHighest;
        }

        void CreateAudioBands()
        {
            for (int i = 0; i < 8; i++)
            {
                if (_freqBand[i] > _freqBandHighest[i])
                {
                    _freqBandHighest[i] = _freqBand[i];
                }
                _audioBand[i] = (_freqBand[i] / _freqBandHighest[i]);
                _audioBandBuffer[i] = (_bandBuffer[i] / _freqBandHighest[i]);
            }
        }

        void GetSpectrumAudioSource()
        {
            // _audioSource.GetSpectrumData(_samples, 0, FFTWindow.Blackman);
            _samples = GetSpectrumData();

        }

        void BandBuffer()
        {
            for (int g = 0; g < 8; ++g)
            {
                if (_freqBand[g] > _bandBuffer[g])
                {
                    _bandBuffer[g] = _freqBand[g];
                    _bufferDecrease[g] = 0.002f;
                }

                if (_freqBand[g] < _bandBuffer[g])
                {
                    _bandBuffer[g] -= _bufferDecrease[g];
                    _bufferDecrease[g] *= 1.2f;

                }
            }
        }


        void MakeFrequencyBands()
        {

            int count = 0;

            for (int i = 0; i < 8; i++)
            {
                float average = 0;
                int sampleCount = (int)Mathf.Pow(2, i) * 2;

                if (i == 7)
                {
                    sampleCount += 2;
                }

                for (int j = 0; j < sampleCount; j++)
                {
                    average += _samples[count] * (count + 1);
                    count++;
                }


                average /= count;
                _freqBand[i] = average * 10;
            }

        }
    }
}
