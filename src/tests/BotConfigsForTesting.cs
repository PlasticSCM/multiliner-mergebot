
namespace MultilinerBot.Tests
{
    internal static class BotConfigsForTesting
    {
        internal static string Full()
        {
            return FULL;
        }

        internal static string OnlyCIPlug()
        {
            return ONLY_CI_PLUG;
        }

        internal static string OnlyCIAndNotificationPlugs()
        {
            return ONLY_CI_AND_NOTIFICATION_PLUGS;
        }

        internal static string OnlyCIAndNotificationPlugsNoPostCheckinPlan()
        {
            return ONLY_CI_AND_NOTIFICATION_PLUGS_NO_AFTER_CI_PLAN;
        }

        internal static string OnlyCIAndTwoNotificationPlugs()
        {
            return ONLY_CI_AND_TWO_NOTIFICATION_PLUGS;
        }

        const string FULL = @"
{
  ""server"": ""localhost:8084"",
  ""repository"": ""assets"",
  ""branch_prefix"": ""AST-"",
  ""merge_to_branches_attr_name"": ""target"",
  ""bot_user"": ""BAEE806DB01"",
  ""plastic_group"": {
    ""status_attribute_group"": {
      ""name"": ""status"",
      ""resolved_value"": ""resolved"",
      ""testing_value"": ""testing"",
      ""failed_value"": ""failed"",
      ""merged_value"": ""merged""
    }
  },
  ""issues_group"": {
    ""plug"": ""tts"",
    ""project_key"": ""AST"",
    ""title_field"": ""title"",
    ""status_field_group"": {
      ""name"": ""status"",
      ""resolved_value"": ""validated"",
      ""testing_value"": ""testing"",
      ""failed_value"": ""open"",
      ""merged_value"": ""closed""
    }
  },
  ""ci_group"": {
    ""plug"": ""My Jenkins"",
    ""plan"": ""debug plan"",
    ""planAfterCheckin"": ""release plan""
  },
  ""notifier_group"": {
    ""notifier1"": {
      ""plug"": ""email"",
      ""user_profile_field"": ""email"",
      ""fixed_recipients"": ""me, you""
    },
    ""notifier2"": {
      ""plug"": """",
      ""user_profile_field"": """",
      ""fixed_recipients"": """"
    }
  }
}
";

        const string ONLY_CI_AND_TWO_NOTIFICATION_PLUGS = @"
{
  ""server"": ""localhost:8084"",
  ""repository"": ""assets"",
  ""branch_prefix"": ""AST-"",
  ""merge_to_branches_attr_name"": ""target"",
  ""bot_user"": ""BAEE806DB01"",
  ""plastic_group"": {
    ""status_attribute_group"": {
      ""name"": ""status"",
      ""resolved_value"": ""resolved"",
      ""testing_value"": ""testing"",
      ""failed_value"": ""failed"",
      ""merged_value"": ""merged""
    }
  },
  ""issues_group"": {
    ""plug"": """",
    ""project_key"": """",
    ""title_field"": """",
    ""status_field_group"": {
      ""name"": """",
      ""resolved_value"": """",
      ""testing_value"": """",
      ""failed_value"": """",
      ""merged_value"": """"
    }
  },
  ""ci_group"": {
    ""plug"": ""My Jenkins"",
    ""plan"": ""debug plan"",
    ""planAfterCheckin"": ""release plan""
  },
  ""notifier_group"": {
    ""notifier1"": {
      ""plug"": ""email"",
      ""user_profile_field"": ""email"",
      ""fixed_recipients"": ""me, you""
    },
    ""notifier2"": {
      ""plug"": ""slack"",
      ""user_profile_field"": ""slack"",
      ""fixed_recipients"": ""adam, eve""
    }
  }
}

";

        const string ONLY_CI_PLUG = @"
{
  ""server"": ""localhost:8084"",
  ""repository"": ""assets"",
  ""branch_prefix"": ""AST-"",
  ""merge_to_branches_attr_name"": ""target"",
  ""bot_user"": ""BAEE806DB01"",
  ""plastic_group"": {
    ""status_attribute_group"": {
      ""name"": ""status"",
      ""resolved_value"": ""resolved"",
      ""testing_value"": ""testing"",
      ""failed_value"": ""failed"",
      ""merged_value"": ""merged""
    }
  },
  ""issues_group"": {
    ""plug"": """",
    ""project_key"": """",
    ""title_field"": """",
    ""status_field_group"": {
      ""name"": ""status"",
      ""resolved_value"": """",
      ""testing_value"": """",
      ""failed_value"": """",
      ""merged_value"": """"
    }
  },
  ""ci_group"": {
    ""plug"": ""My Jenkins"",
    ""plan"": ""debug plan"",
    ""planAfterCheckin"": ""release plan""
  },
  ""notifier_group"": {
    ""notifier1"": {
      ""plug"": """",
      ""user_profile_field"": """",
      ""fixed_recipients"": """"
    },
    ""notifier2"": {
      ""plug"": """",
      ""user_profile_field"": ""field"",
      ""fixed_recipients"": """"
    }
  }
}
";


        const string ONLY_CI_AND_NOTIFICATION_PLUGS = @"
{
  ""server"": ""localhost:8084"",
  ""repository"": ""assets"",
  ""branch_prefix"": ""AST-"",
  ""merge_to_branches_attr_name"": ""target"",
  ""bot_user"": ""BAEE806DB01"",
  ""plastic_group"": {
    ""status_attribute_group"": {
      ""name"": ""status"",
      ""resolved_value"": ""resolved"",
      ""testing_value"": ""testing"",
      ""failed_value"": ""failed"",
      ""merged_value"": ""merged""
    }
  },
  ""issues_group"": {
    ""plug"": """",
    ""project_key"": """",
    ""title_field"": """",
    ""status_field_group"": {
      ""name"": """",
      ""resolved_value"": """",
      ""testing_value"": """",
      ""failed_value"": """",
      ""merged_value"": """"
    }
  },
  ""ci_group"": {
    ""plug"": ""My Jenkins"",
    ""plan"": ""debug plan"",
    ""planAfterCheckin"": ""release plan""
  },
  ""notifier_group"": {
    ""notifier1"": {
      ""plug"": ""email"",
      ""user_profile_field"": ""email"",
      ""fixed_recipients"": """"
    },
    ""notifier2"": {
      ""plug"": """",
      ""user_profile_field"": """",
      ""fixed_recipients"": ""me, you""
    }
  }
}

";

        const string ONLY_CI_AND_NOTIFICATION_PLUGS_NO_AFTER_CI_PLAN = @"
{
  ""server"": ""localhost:8084"",
  ""repository"": ""assets"",
  ""branch_prefix"": ""AST-"",
  ""merge_to_branches_attr_name"": ""target"",
  ""bot_user"": ""BAEE806DB01"",
  ""plastic_group"": {
    ""status_attribute_group"": {
      ""name"": ""status"",
      ""resolved_value"": ""resolved"",
      ""testing_value"": ""testing"",
      ""failed_value"": ""failed"",
      ""merged_value"": ""merged""
    }
  },
  ""issues_group"": {
    ""plug"": """",
    ""project_key"": """",
    ""title_field"": """",
    ""status_field_group"": {
      ""name"": """",
      ""resolved_value"": """",
      ""testing_value"": """",
      ""failed_value"": """",
      ""merged_value"": """"
    }
  },
  ""ci_group"": {
    ""plug"": ""My Jenkins"",
    ""plan"": ""debug plan"",
    ""planAfterCheckin"": """"
  },
  ""notifier_group"": {
    ""notifier1"": {
      ""plug"": ""email"",
      ""user_profile_field"": """",
      ""fixed_recipients"": ""me""
    },
    ""notifier2"": {
      ""plug"": """",
      ""user_profile_field"": """",
      ""fixed_recipients"": ""me""
    }
  }
}

";
    }
}
