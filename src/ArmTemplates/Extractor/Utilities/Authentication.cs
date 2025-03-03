﻿using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Constants;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities
{
    public class Authentication
    {
        internal static bool isTokenValid = false;
        public static DateTime start = DateTime.Now;
        internal static string internalAzToken;
        internal static string internalAzSubscriptionId;
        public async Task<(string azToken, string azSubscriptionId)> GetAccessToken()
        {
            var tokenTimeout = DateTime.Now;

            if ((tokenTimeout - start).TotalMinutes <= 15 && isTokenValid)
            {
                return (internalAzToken, internalAzSubscriptionId);
            }

            (bool cliTokenSucceeded, string cliToken) = await this.TryGetAzCliToken();
            (bool cliSubscriptionIdSucceeded, string cliSubscriptionId) = await this.TryGetAzSubscriptionId();

            if (cliTokenSucceeded && cliSubscriptionIdSucceeded)
            {
                start = DateTime.Now;
                internalAzToken = cliToken;
                internalAzSubscriptionId = cliSubscriptionId;
                isTokenValid = true;
                return (cliToken, cliSubscriptionId);
            }

            throw new Exception("Unable to connect to Azure. Make sure you have the `az` CLI or Azure PowerShell installed and logged in and try again");
        }

        async Task<(bool succeeded, string token)> TryGetAzCliToken()
        {
            return await ExecuteCommand(GlobalConstants.azAccessToken);
        }

        async Task<(bool succeeded, string token)> TryGetAzSubscriptionId()
        {
            return await ExecuteCommand(GlobalConstants.azSubscriptionId);
        }

        static async Task<(bool succeeded, string token)> ExecuteCommand(string commandParameters)
        {
            var az = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new Executable("cmd", "/c az " + commandParameters)
                : new Executable("az", commandParameters);

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var completed = az.RunAsync(o => stdout.AppendLine(o), e => stderr.AppendLine(e));

            if (await completed == 0)
                return (true, stdout.ToString().Trim(' ', '\n', '\r', '"'));
            else
            {
                Logger.LogError($"Unable to fetch access token from az cli. Error: {stderr.ToString().Trim(' ', '\n', '\r')}");
                return (false, stdout.ToString().Trim(' ', '\n', '\r', '"'));
            }
        }
    }
}
