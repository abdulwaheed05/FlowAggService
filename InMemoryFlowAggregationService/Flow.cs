using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataFlowService.Models
{
    public class Flow
    {
        [JsonPropertyName("src_app")]
        [Required]
        public string SrcApp { get; set; }
        
        [JsonPropertyName("dest_app")]
        [Required]
        public string DestApp { get; set; }

        [JsonPropertyName("vpc_id")]
        [Required]
        public string VpcId { get; set; }

        [JsonPropertyName("bytes_rx")]
        public int BytesRx { get; set; }

        [JsonPropertyName("bytes_tx")]
        public int BytesTx { get; set; }

        [JsonPropertyName("hour")]
        [Range(1,24)]
        public int Hour { get; set; }
    }
}
