﻿// The FinderOuter
// Copyright (c) 2020 Coding Enthusiast
// Distributed under the MIT software license, see the accompanying
// file LICENCE or http://www.opensource.org/licenses/mit-license.php.

using FinderOuter.Backend.ECC;

namespace FinderOuter.Services.Comparers
{
    public abstract class PrvToAddrBase : ICompareService
    {
        protected byte[] hash;

        public virtual bool Init(string address)
        {
            AddressService serv = new();
            return serv.CheckAndGetHash(address, out hash);
        }

        public abstract ICompareService Clone();

        protected readonly Calc _calc = new();
        public Calc Calc => _calc;

        public abstract unsafe bool Compare(uint* hPt);
        public abstract unsafe bool Compare(ulong* hPt);
        public abstract bool Compare(in PointJacobian point);

        public bool Compare(byte[] key)
        {
            Scalar k = new(key, out int overflow);
            if (overflow != 0)
            {
                return false;
            }
            PointJacobian pt = _calc.MultiplyByG(k);
            return Compare(pt);
        }

        public bool Compare(Scalar key) => Compare(Calc.MultiplyByG(key));
    }
}
