using System;

namespace cnMaestro
{
    public struct eipResponse
    {
        public apiDevice device { get; set; }
        public apiRadio statistics { get; set; }
    }

    public struct apiResponse<T>
    {
        public apiPaging paging { get; set; }
        public T[] data { get; set; }
    }

    public struct apiPaging
    {
        public int offset { get; set; }
        public int limit { get; set; }
        public int total { get; set; }
    }

    public struct apiDevice
    {
        public string product { get; set; }
        public string tower { get; set; }
        public string name { get; set; }
        public string software_version { get; set; }
        public string status { get; set; }
        public Int64 status_time { get; set; }
        public string ip { get; set; }
    }

    public struct apiStatistics
    {
        public apiRadio radio { get; set; }
    }

    public struct apiRadio
    {
        public int dl_snr_v { get; set; }
        public int dl_snr_h { get; set; }
        public int ul_snr_v { get; set; }
        public int ul_snr_h { get; set; }
        public float ul_rssi { get; set; }
        public float dl_rssi { get; set; }
        public string dl_modulation { get; set; }
        public string ul_modulation { get; set; }
        public int ul_lqi { get; set; }
        public int dl_lqi { get; set; }
    }
}
