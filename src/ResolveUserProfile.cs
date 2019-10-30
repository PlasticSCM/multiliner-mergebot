using System;
using System.Collections.Generic;
using System.Linq;

using log4net;
using Newtonsoft.Json.Linq;

using MultilinerBot.Api.Interfaces;

namespace MultilinerBot
{
    internal static class ResolveUserProfile
    {
        internal static List<string> ResolveFieldForUsers(
            IRestApi restApi,
            List<string> users,
            string profileFieldQualifiedName)
        {
            IEnumerable<string> uniqueUsers = users.Distinct();

            if (string.IsNullOrEmpty(profileFieldQualifiedName))
                return uniqueUsers.ToList();

            string[] profileFieldsPath = profileFieldQualifiedName.Split(
                new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            return uniqueUsers.Select(user =>
            {
                string solvedUser;

                if (!TryResolveUserProfileValue(
                    restApi, user, profileFieldsPath, out solvedUser))
                {
                    return user;
                }

                return solvedUser;
            }).Distinct().ToList();
        }

        static bool TryResolveUserProfileValue(
            IRestApi restApi, 
            string user, 
            string[] profileFieldsPath, 
            out string solvedUser)
        {
            solvedUser = null;

            JObject userProfileResponse = GetUserProfile(restApi, user);

            if (userProfileResponse == null)
                return false;

            solvedUser = ParseUserProfile.GetFieldFromProfile(
                userProfileResponse, profileFieldsPath);

            return !string.IsNullOrEmpty(solvedUser);
        }

        static JObject GetUserProfile(IRestApi restApi, string user)
        {
            try
            {
                return MultilinerMergebotApi.Users.GetUserProfile(restApi, user);
            }
            catch (Exception e)
            {
                mLog.WarnFormat(
                    "Unable to resolve user's profile for username '{0}': {1}",
                    user, e.Message);

                return null;
            }
        }

        static readonly ILog mLog = LogManager.GetLogger("ResolveUserProfile");
    }
}
