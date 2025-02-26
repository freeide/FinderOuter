﻿// The FinderOuter
// Copyright (c) 2020 Coding Enthusiast
// Distributed under the MIT software license, see the accompanying
// file LICENCE or http://www.opensource.org/licenses/mit-license.php.

using Autarkysoft.Bitcoin;
using Autarkysoft.Bitcoin.Cryptography.Asymmetric.KeyPairs;
using FinderOuter.Backend.Hashing;
using FinderOuter.Backend.ECC;
using System;

namespace FinderOuter.Services.Comparers
{
    /// <summary>
    /// Compares 2 private key bytes. It is useful for HD keys where user has a single child private key.
    /// </summary>
    public class PrvToPrvComparer : ICompareService
    {
        private byte[] expectedBytes;
        private Scalar expectedKey;

        public bool Init(string data)
        {
            try
            {
                using PrivateKey temp = new(data);
                expectedBytes = temp.ToBytes();
                expectedKey = new(expectedBytes, out _);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public ICompareService Clone()
        {
            return new PrvToPrvComparer()
            {
                expectedBytes = this.expectedBytes.CloneByteArray(),
                expectedKey = this.expectedKey
            };
        }

        private readonly Calc _calc = new();
        public Calc Calc => _calc;
        public unsafe bool Compare(uint* hPt) => ((Span<byte>)expectedBytes).SequenceEqual(Sha256Fo.GetBytes(hPt));
        public unsafe bool Compare(ulong* hPt) => ((Span<byte>)expectedBytes).SequenceEqual(Sha512Fo.GetFirst32Bytes(hPt));

        public bool Compare(byte[] key) => ((ReadOnlySpan<byte>)expectedBytes).SequenceEqual(key);

        public bool Compare(Scalar key) => key == expectedKey;

        public bool Compare(in PointJacobian point) => throw new NotImplementedException();
    }
}
