using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace StalkR
{
    [DataContract]
    public class Friend
    {
        [DataMember(Name = "id")]
        public int id { get; set; }

        [DataMember(Name = "user_id")]
        public int user_id { get; set; }

        [DataMember(Name = "first_name")]
        public string first_name { get; set; }

        [DataMember(Name = "last_name")]
        public string last_name { get; set; }

        [DataMember(Name = "email")]
        public string email { get; set; }

        [DataMember(Name = "phone")]
        public string phone { get; set; }
    }

    [DataContract]
    public class Response
    {
        [DataMember(Name = "error")]
        public string error { get; set; }

        [DataMember(Name = "friend")]
        public Friend friend { get; set; }
    }
}
