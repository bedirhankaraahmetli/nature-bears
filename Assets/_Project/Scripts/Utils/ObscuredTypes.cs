using System;
using NatureBears.Core;
using UnityEngine;

namespace NatureBears.Utils
{
    /// <summary>
    /// Session-wide XOR key shared by the obscured types. Regenerated every
    /// launch (and on play-mode enter with domain reload disabled) so memory
    /// scanners can't rely on a constant mask.
    /// </summary>
    internal static class ObscuredKey
    {
        internal static long Key = Generate();

        private static long Generate()
        {
            var bytes = new byte[8];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Key = Generate();
        }
    }

    /// <summary>
    /// In-memory obfuscated double for critical currencies (Golden Honey,
    /// Slumber Points). Defeats naive memory-scanner cheats: the real value is
    /// XOR-masked, a checksum detects direct writes, and a plain-value decoy
    /// field is left for scanners to find and edit harmlessly.
    /// NOT persisted — SaveData stores plain doubles (the file is AES encrypted).
    /// </summary>
    [Serializable]
    public struct ObscuredDouble
    {
        private const long ChecksumMagic = unchecked((long)0xBEA4B0A4CAFE1234);

        private long _obfuscatedBits;
        private long _checksum;
        private bool _initialized;

        /// <summary>Decoy: intentionally scannable/editable. Never read for logic.</summary>
        public double honeypot;

        public ObscuredDouble(double value)
        {
            _obfuscatedBits = 0;
            _checksum = 0;
            _initialized = true;
            honeypot = value;
            Set(value);
        }

        private void Set(double value)
        {
            _obfuscatedBits = BitConverter.DoubleToInt64Bits(value) ^ ObscuredKey.Key;
            _checksum = _obfuscatedBits ^ ChecksumMagic;
            _initialized = true;
            honeypot = value;
        }

        private double Get()
        {
            if (!_initialized)
                return 0;

            if (_checksum != (_obfuscatedBits ^ ChecksumMagic))
            {
                SignalBus.Fire(new CurrencyTamperDetectedSignal());
                return 0;
            }

            return BitConverter.Int64BitsToDouble(_obfuscatedBits ^ ObscuredKey.Key);
        }

        public static implicit operator double(ObscuredDouble o) => o.Get();
        public static implicit operator ObscuredDouble(double value) => new ObscuredDouble(value);

        public override string ToString() => Get().ToString();
    }

    /// <summary>Integer counterpart of <see cref="ObscuredDouble"/> (counters, timestamps).</summary>
    [Serializable]
    public struct ObscuredLong
    {
        private const long ChecksumMagic = unchecked((long)0x5EED5A17B00F5678);

        private long _obfuscatedBits;
        private long _checksum;
        private bool _initialized;

        /// <summary>Decoy: intentionally scannable/editable. Never read for logic.</summary>
        public long honeypot;

        public ObscuredLong(long value)
        {
            _obfuscatedBits = 0;
            _checksum = 0;
            _initialized = true;
            honeypot = value;
            Set(value);
        }

        private void Set(long value)
        {
            _obfuscatedBits = value ^ ObscuredKey.Key;
            _checksum = _obfuscatedBits ^ ChecksumMagic;
            _initialized = true;
            honeypot = value;
        }

        private long Get()
        {
            if (!_initialized)
                return 0;

            if (_checksum != (_obfuscatedBits ^ ChecksumMagic))
            {
                SignalBus.Fire(new CurrencyTamperDetectedSignal());
                return 0;
            }

            return _obfuscatedBits ^ ObscuredKey.Key;
        }

        public static implicit operator long(ObscuredLong o) => o.Get();
        public static implicit operator ObscuredLong(long value) => new ObscuredLong(value);

        public override string ToString() => Get().ToString();
    }
}
