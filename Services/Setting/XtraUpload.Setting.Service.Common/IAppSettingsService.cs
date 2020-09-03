﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using XtraUpload.Domain;

namespace XtraUpload.Setting.Service.Common
{
    public interface IAppSettingsService
    {
        /// <summary>
        /// Read Appsettings configuraion
        /// </summary>
        ReadAppSettingResult ReadAppSetting();
    }
}
