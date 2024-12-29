using System;
using UnityEngine;

namespace AdminTools.Models
{
    public class TemplateItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public string icon { get; set; }
        public string description { get; set; }
        public int price { get; set; }
        public string bundle { get; set; }
    }

    public class ItemData : MonoBehaviour
    {
        public string id;
    }

    public class ItemInfoResponse
    {
        public TemplateItem data { get; set; }
    }
}
