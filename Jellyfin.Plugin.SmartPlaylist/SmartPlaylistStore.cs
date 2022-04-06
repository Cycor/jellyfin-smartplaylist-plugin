using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SmartPlaylist
{
    public interface ISmartPlaylistStore
    {
        Task<SmartPlaylistDto> GetSmartPlaylistAsync(Guid smartPlaylistId);
        Task<SmartPlaylistDto[]> LoadPlaylistsAsync(Guid userId);
        Task<SmartPlaylistDto[]> GetAllSmartPlaylistsAsync();
        Task Save(SmartPlaylistDto smartPlaylist, CancellationToken token);
        void Delete(Guid userId, string smartPlaylistId);
    }

    public class SmartPlaylistStore : ISmartPlaylistStore
    {
        private readonly ISmartPlaylistFileSystem _fileSystem;

        public SmartPlaylistStore(ISmartPlaylistFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }


        public async Task<SmartPlaylistDto> GetSmartPlaylistAsync(Guid smartPlaylistId)
        {
            var fileName = _fileSystem.GetSmartPlaylistFilePath(smartPlaylistId.ToString());

            return await LoadPlaylistAsync(fileName).ConfigureAwait(false);
        }

        public async Task<SmartPlaylistDto[]> LoadPlaylistsAsync(Guid userId)
        {
            var deserializeTasks = _fileSystem.GetSmartPlaylistFilePaths(userId.ToString()).Select(LoadPlaylistAsync).ToArray();

            await Task.WhenAll(deserializeTasks).ConfigureAwait(false);

            return deserializeTasks.Select(x => x.Result).ToArray();
        }

        public async Task<SmartPlaylistDto[]> GetAllSmartPlaylistsAsync()
        {
            var deserializeTasks = _fileSystem.GetAllSmartPlaylistFilePaths().Select(LoadPlaylistAsync).ToArray();

            await Task.WhenAll(deserializeTasks).ConfigureAwait(false);

            return deserializeTasks.Select(x => x.Result).ToArray();
        }

        public async Task Save(SmartPlaylistDto smartPlaylist, CancellationToken token)
        {
            var filePath = _fileSystem.GetSmartPlaylistPath(smartPlaylist.Id, smartPlaylist.FileName);

            await using var writeStream = File.Open(filePath, FileMode.Create);
            var options = new JsonSerializerOptions { WriteIndented = true};

            await JsonSerializer.SerializeAsync(writeStream, smartPlaylist, options, cancellationToken: token).ConfigureAwait(false);
        }

        public void Delete(Guid userId, string smartPlaylistId)
        {
            var filePath = _fileSystem.GetSmartPlaylistPath(userId.ToString(), smartPlaylistId);
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        private async Task<SmartPlaylistDto> LoadPlaylistAsync(string filePath)
        {
            using var reader = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.Asynchronous);
            var res = await JsonSerializer.DeserializeAsync<SmartPlaylistDto>(reader).ConfigureAwait(false);
            return res;
        }
    }
}