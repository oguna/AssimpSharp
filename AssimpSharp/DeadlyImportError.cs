﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssimpSharp
{
    public class DeadlyImportError : Exception
    {
        public DeadlyImportError(string errorText)
            :base(errorText)
        {
        }
    }
}
