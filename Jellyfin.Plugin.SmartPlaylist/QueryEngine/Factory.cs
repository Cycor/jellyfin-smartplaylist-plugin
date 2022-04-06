using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using Jellyfin.Data.Entities;
using System.Linq;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SmartPlaylist.QueryEngine
{
    class OperandFactory
    {
        // Returns a specific operand povided a baseitem, user, and library manager object.
        public static Operand GetMediaType(ILibraryManager libraryManager, BaseItem baseItem, User user, ILogger logger)
        {

            var operand = new Operand(baseItem.Name);
            var directors = new List<string> { };
            var people = libraryManager.GetPeople(baseItem);
            if (people.Any())
            {
                // Maps to MediaBrowser.Model.Entities.PersonType
                operand.Actors = people.Where(x => x.Type.Equals("Actor")).Select(x => x.Name).ToList();
                operand.Composers = people.Where(x => x.Type.Equals("Composer")).Select(x => x.Name).ToList();
                operand.Directors = people.Where(x => x.Type.Equals("Director")).Select(x => x.Name).ToList();
                operand.GuestStars = people.Where(x => x.Type.Equals("GuestStar")).Select(x => x.Name).ToList();
                operand.Producers = people.Where(x => x.Type.Equals("Producer")).Select(x => x.Name).ToList();
                operand.Writers = people.Where(x => x.Type.Equals("Writer")).Select(x => x.Name).ToList();
            }

            operand.Tags = baseItem.Tags?.ToList();
            operand.Genres = baseItem.Genres.ToList();
            operand.IsPlayed = baseItem.IsPlayed(user);
            operand.Studios = baseItem.Studios.ToList();
            operand.CommunityRating = baseItem.CommunityRating.GetValueOrDefault();
            operand.CriticRating = baseItem.CriticRating.GetValueOrDefault();
            operand.MediaType = baseItem.MediaType;
            operand.Album = baseItem.Album;

            if (baseItem.PremiereDate.HasValue)
            {
                operand.PremiereDate = Process(baseItem.PremiereDate.Value)?.ToUnixTimeSeconds() ?? 0;
            }

            operand.DateCreated = Process(baseItem.DateCreated)?.ToUnixTimeSeconds() ?? 0;
            operand.DateLastRefreshed = Process(baseItem.DateLastRefreshed)?.ToUnixTimeSeconds() ?? 0;
            operand.DateLastSaved = Process(baseItem.DateLastSaved)?.ToUnixTimeSeconds() ?? 0;
            operand.DateModified = Process(baseItem.DateModified)?.ToUnixTimeSeconds() ?? 0;

            operand.FolderPath = baseItem.ContainingFolderPath ?? "";
            return operand;
        }

        private static DateTimeOffset? Process(DateTime dateTime)
        {
            try
            {
                if (dateTime.Year > 1900 && dateTime.Year < 5000)
                    return new DateTimeOffset(dateTime);
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
