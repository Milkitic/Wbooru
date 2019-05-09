﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wbooru.Settings
{
    public class SettingBase
    {
        public virtual void OnAfterLoad() { }
        public virtual void OnBeforeSave() { }
    }
}
