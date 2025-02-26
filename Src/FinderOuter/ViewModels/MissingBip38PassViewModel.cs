﻿// The FinderOuter
// Copyright (c) 2020 Coding Enthusiast
// Distributed under the MIT software license, see the accompanying
// file LICENCE or http://www.opensource.org/licenses/mit-license.php.

using FinderOuter.Backend;
using FinderOuter.Models;
using FinderOuter.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FinderOuter.ViewModels
{
    public class MissingBip38PassViewModel : OptionVmBase
    {
        public MissingBip38PassViewModel()
        {
            Bip38Service = new(Result);
            InputTypeList = ListHelper.GetEnumDescItems(new InputType[] { InputType.PrivateKey }).ToArray();
            SelectedInputType = InputTypeList.First();
            PassRecoveryModeList = ListHelper.GetEnumDescItems<PassRecoveryMode>().ToArray();
            SelectedPassRecoveryMode = PassRecoveryModeList.First();

            IObservable<bool> isFindEnabled = this.WhenAnyValue(
                x => x.Bip38,
                x => x.CompareString,
                x => x.Result.CurrentState,
                (mn, extra, state) =>
                            !string.IsNullOrEmpty(mn) &&
                            !string.IsNullOrEmpty(extra) &&
                            state != State.Working);

            FindCommand = ReactiveCommand.Create(Find, isFindEnabled);

            this.WhenAnyValue(x => x.SelectedPassRecoveryMode.Value)
                .Subscribe(x => IsCheckBoxVisible = x == PassRecoveryMode.Alphanumeric);

            HasExample = true;
            IObservable<bool> isExampleVisible = this.WhenAnyValue(
                x => x.Result.CurrentState,
                (state) => state != State.Working && HasExample);
            ExampleCommand = ReactiveCommand.Create(Example, isExampleVisible);

            SetExamples(GetExampleData());
        }


        public override string OptionName => "Missing BIP38 pass";
        public override string Description => $"This option can recover BIP-38 encryption password.{Environment.NewLine}" +
            $"Note that since BIP-38 algorithm is designed to be very expensive, hence this option is very slow at recovering " +
            $"passwords. Don't expect more than 3 or 4 checks per second per thread (the more CPU/cores you have the faster " +
            $"it will be).";

        public Bip38Service Bip38Service { get; }
        public IPasswordService PassService { get; set; } = new PasswordService();
        public IEnumerable<DescriptiveItem<InputType>> InputTypeList { get; }
        public IEnumerable<DescriptiveItem<PassRecoveryMode>> PassRecoveryModeList { get; }

        private DescriptiveItem<InputType> _inT;
        public DescriptiveItem<InputType> SelectedInputType
        {
            get => _inT;
            set => this.RaiseAndSetIfChanged(ref _inT, value);
        }

        private DescriptiveItem<PassRecoveryMode> _recMode;
        public DescriptiveItem<PassRecoveryMode> SelectedPassRecoveryMode
        {
            get => _recMode;
            set => this.RaiseAndSetIfChanged(ref _recMode, value);
        }

        // TODO: this may be better as a different view model and a user interface loaded in UI so that we can have more than
        // 2 choices (for now it works fine!)
        private bool _isChkVisible;
        public bool IsCheckBoxVisible
        {
            get => _isChkVisible;
            set => this.RaiseAndSetIfChanged(ref _isChkVisible, value);
        }

        private string _bip38;
        public string Bip38
        {
            get => _bip38;
            set => this.RaiseAndSetIfChanged(ref _bip38, value);
        }

        private int _passLen = 1;
        public int PassLength
        {
            get => _passLen;
            set
            {
                if (value < 1)
                    value = 1;

                this.RaiseAndSetIfChanged(ref _passLen, value);
            }
        }

        private string _customChars;
        public string CustomChars
        {
            get => _customChars;
            set => this.RaiseAndSetIfChanged(ref _customChars, value);
        }

        private string _comp;
        public string CompareString
        {
            get => _comp;
            set => this.RaiseAndSetIfChanged(ref _comp, value);
        }

        private bool _isUpper;
        public bool IsUpperCase
        {
            get => _isUpper;
            set => this.RaiseAndSetIfChanged(ref _isUpper, value);
        }

        private bool _isLower;
        public bool IsLowerCase
        {
            get => _isLower;
            set => this.RaiseAndSetIfChanged(ref _isLower, value);
        }

        private bool _isNum;
        public bool IsNumber
        {
            get => _isNum;
            set => this.RaiseAndSetIfChanged(ref _isNum, value);
        }

        private bool _isSymbol;
        public bool IsSymbol
        {
            get => _isSymbol;
            set => this.RaiseAndSetIfChanged(ref _isSymbol, value);
        }

        private bool _useSpace;
        public bool UseSpace
        {
            get => _useSpace;
            set => this.RaiseAndSetIfChanged(ref _useSpace, value);
        }

        public static string AllSymbols => $"Symbols ({ConstantsFO.AllSymbols})";


        public PasswordType PassType
        {
            get
            {
                PasswordType result = PasswordType.None;
                if (IsUpperCase)
                {
                    result |= PasswordType.UpperCase;
                }
                if (IsLowerCase)
                {
                    result |= PasswordType.LowerCase;
                }
                if (IsNumber)
                {
                    result |= PasswordType.Numbers;
                }
                if (IsSymbol)
                {
                    result |= PasswordType.Symbols;
                }
                if (UseSpace)
                {
                    result |= PasswordType.Space;
                }

                return result;
            }
        }



        public override void Find()
        {
            byte[] allValues = null;
            string error = null;
            bool success = SelectedPassRecoveryMode.Value switch
            {
                PassRecoveryMode.Alphanumeric => PassService.TryGetAllValues(PassType, out allValues, out error),
                PassRecoveryMode.CustomChars => PassService.TryGetAllValues(CustomChars, out allValues, out error),
                _ => false,
            };

            if (success)
            {
                Bip38Service.Find(Bip38, CompareString, SelectedInputType.Value, PassLength, allValues); 
            }
            else
            {
                Result.Init();
                Result.Fail(error);
            }
        }


        public void Example()
        {
            object[] ex = GetNextExample();

            Bip38 = (string)ex[0];

            int temp = (int)ex[1];
            Debug.Assert(temp < InputTypeList.Count());
            SelectedInputType = InputTypeList.ElementAt(temp);

            CompareString = (string)ex[2];
            PassLength = (int)ex[3];

            temp = (int)ex[4];
            Debug.Assert(temp < PassRecoveryModeList.Count());
            SelectedPassRecoveryMode = PassRecoveryModeList.ElementAt(temp);

            CustomChars = (string)ex[5];

            PasswordType flag = (PasswordType)(ulong)ex[6];
            IsUpperCase = flag.HasFlag(PasswordType.UpperCase);
            IsLowerCase = flag.HasFlag(PasswordType.LowerCase);
            IsNumber = flag.HasFlag(PasswordType.Numbers);
            IsSymbol = flag.HasFlag(PasswordType.Symbols);

            Result.Message = $"Example {exampleIndex} of {totalExampleCount}. Source: {(string)ex[7]}";
        }

        private ExampleData GetExampleData()
        {
            return new ExampleData<string, int, string, int, int, string, ulong, string>()
            {
                {
                    "6PRSR1GPq9Y7a6cCDwR2EshQGHXF4tWqGKHy2uU3qwRpcw4zZA4zz7GT1W",
                    1, // InputType
                    "1PSuGX1gXt8iu7gftMVsLg66EVuA1fRDz2",
                    2, // Pass length
                    0, // Recovery mode
                    "",
                    2, // Pass type flag
                    $"Random.{Environment.NewLine}" +
                    $"This example is a BIP38 with a very simple password using only lower case letters (ab)." +
                    $"{Environment.NewLine}" +
                    $"Estimated time: <1 sec"
                },
                {
                    "6PRKDN49yuCFZ5gzPq4iGY7Av9FZ1YEXXpgsDXTsXEMjZoUVMLtzXBxw5Q",
                    1, // InputType
                    "13TQwKK5vsxziCKJucwhATRqi23ogkAh66",
                    4, // Pass length
                    1, // Recovery mode
                    "!jRrSs",
                    0, // Pass type flag
                    $"Random.{Environment.NewLine}" +
                    $"This example is a BIP38 with a longer password using lower and upper case letters and symbols (j!RS). " +
                    $"But we know the possible characters used in the password so the recovery is a lot faster." +
                    $"{Environment.NewLine}" +
                    $"Estimated time: ~20 sec"
                },
            };
        }
    }
}
