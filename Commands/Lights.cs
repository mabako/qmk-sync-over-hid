using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LilyHid.Commands
{
    internal class Lights : ICommand, IDisposable
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        private readonly HttpClient _httpClient = new HttpClient();
        private Configuration _configuration;

        public void Configure(IConfigurationSection configurationSection)
        {
            _configuration = configurationSection.Get<Configuration>();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
        }

        public void Register(QmkCommunication qmkCommunication)
        {
            if (_configuration.IsValid)
            {
                qmkCommunication.OnCommandReceived += OnCommandReceived;
            }
        }

        private void OnCommandReceived(object sender, QmkCommunication.CommandEventArgs e)
        {
            if (e.CommandId == QmkReceivedCommandId.IncreaseLights || e.CommandId == QmkReceivedCommandId.DecreaseLights)
            {
                Task.Run(async () => await ChangeLightLevel(e.CommandId));
            }
        }

        private async Task ChangeLightLevel(QmkReceivedCommandId commandId)
        {
            int currentLevel = await GetCurrentLightLevelAsync();
            int nextLevel;
            if (commandId == QmkReceivedCommandId.IncreaseLights)
            {
                nextLevel = _configuration.Levels
                    .Where(level => level > currentLevel)
                    .DefaultIfEmpty(_configuration.Levels.Last())
                    .First();
            }
            else
            {
                nextLevel = _configuration.Levels
                    .Where(level => level < currentLevel)
                    .DefaultIfEmpty(_configuration.Levels.First())
                    .Last();
            }

            if (currentLevel == nextLevel)
                return;

            Keylight nextState = Keylight.FromLevel(nextLevel);
            await SetCurrentLightLevelAsync(nextState);
        }

        private async Task<int> GetCurrentLightLevelAsync()
        {
            var levels = await Task.WhenAll(_configuration.Addresses.Select(async address => await GetCurrentLightLevelAsync(address)));
            return levels.DefaultIfEmpty(20).Min();
        }

        private async Task<int> GetCurrentLightLevelAsync(string address)
        {
            var keylightRoot = await _httpClient.GetFromJsonAsync<KeylightRoot>(address, _serializerOptions);
            return keylightRoot.Lights.Min(light => light.ToLevel());
        }

        /// <summary>
        /// This probably only sets the first light of each keylight, but all mine only have 1 light so I can't verify that.
        /// </summary>
        private async Task SetCurrentLightLevelAsync(Keylight keylight)
        {
            await Task.WhenAll(_configuration.Addresses.Select(async address => await SetCurrentLightLevelAsync(address, keylight)));
        }
        private async Task SetCurrentLightLevelAsync(string address, Keylight keylight)
        {
            var json = JsonSerializer.Serialize(
                new KeylightRoot
                {
                    Lights = new List<Keylight> { keylight }
                },
                _serializerOptions);
            var result = await _httpClient.PutAsync(address, new StringContent(json));
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private class Configuration
        {
            public List<string> Addresses { get; set; } = new List<string>();
            public List<int> Levels { get; set; } = new List<int>();

            internal bool IsValid => Addresses.Count >= 0 && Levels.Count >= 0;
        }

        private class KeylightRoot
        {
            public List<Keylight> Lights { get; set; }
        }

        private class Keylight
        {
            public int On { get; set; }
            public int? Brightness { get; set; }

            public static Keylight FromLevel(int level)
            {
                return new Keylight
                {
                    On = level >= 3 ? 1 : 0,
                    Brightness = level >= 3 ? level : null,
                };
            }

            public int ToLevel()
            {
                return On * Brightness ?? 0;
            }
        }
    }
}
