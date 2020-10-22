using System;
using System.Globalization;

namespace FsElo.WebApp.Application
{
    public class UserInfo
    {
        public string User { get; set; }
        
        public CultureInfo Culture { get; set; }
        
        public TimeSpan UtcOffset { get; set; }
    }
}