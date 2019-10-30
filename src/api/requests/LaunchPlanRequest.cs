﻿using System.Collections.Generic;

namespace MultilinerBot.Api.Requests
{
    public class LaunchPlanRequest
    {
        public string ObjectSpec { get; set; }
        public string Comment { get; set; }

        public Dictionary<string, string> Properties { get; set; }
    }
}
