using System;
using System.Collections.Generic;
using System.Text;

namespace AChatFull
{
    public class DocumentInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }          // bytes
        public string MimeType { get; set; }
        public string RemoteUrl { get; set; }         // URL на сервере
        public string LocalPath { get; set; }         // куда скачали/скопировали
        public bool IsDownloaded { get; set; }
        public bool IsDownloading { get; set; }
        public double Progress { get; set; }          // 0..1
    }
}
