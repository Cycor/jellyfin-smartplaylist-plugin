using System;
using Xunit;
using Jellyfin.Plugin.SmartPlaylist;
using Jellyfin.Plugin.SmartPlaylist.QueryEngine;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Tests
{
    public class SmartPlaylistTest
    {
        [Fact]
        public void DtoToSmartPlaylist()
        {
            var dto = new SmartPlaylistDto();
            dto.Id = "87ccaa10-f801-4a7a-be40-46ede34adb22";
            dto.Name = "Foo";
            dto.User = "Rob";

            var es = new ExpressionSet();
            es.Expressions = new List<Expression>
            {
                new Expression("foo", "bar", "biz")
            };
            dto.ExpressionSets = new List<ExpressionSet> { es };
            dto.Order = new OrderDto { Name = "Release Date Descending" };

            SmartPlaylist smart_playlist = new SmartPlaylist(dto);

            Assert.Equal(1000, smart_playlist.MaxItems);
            Assert.Equal("87ccaa10-f801-4a7a-be40-46ede34adb22", smart_playlist.Id);
            Assert.Equal("Foo", smart_playlist.Name);
            Assert.Equal("Rob", smart_playlist.User);
            Assert.Equal("foo", smart_playlist.ExpressionSets[0].Expressions[0].MemberName);
            Assert.Equal("bar", smart_playlist.ExpressionSets[0].Expressions[0].Operator);
            Assert.Equal("biz", smart_playlist.ExpressionSets[0].Expressions[0].TargetValue);
            Assert.Equal("PremiereDateOrderDesc", smart_playlist.Order.GetType().Name);


        }

        [Fact]
        public void EngineTest()
        {
            var dto = new SmartPlaylistDto();
            dto.Id = "87ccaa10-f801-4a7a-be40-46ede34adb22";
            dto.Name = "Foo";
            dto.User = "Rob";

            var es = new ExpressionSet();
            es.Name = "Test";
            es.Expressions = new List<Expression>
            {

                new Expression("Name", "Equal", "Foo"),  // NET operator
                new Expression("Name", "!Equal", "foo"), // NET operator with NOT
                new Expression("Name", "Equal", "foo") { IgnoreCase = true },  // NET operator with IgnoreCase
                new Expression("DateCreated", "MatchRegex", "100"), // Custom function with tostring requirement
                new Expression("DateCreated", "Not MatchRegex", "105"),  // Custom function with tostring requirement and NOT
                new Expression("DateCreated", "NotMatchRegex", "105"),  // Custom function with tostring requirement and attached NOT
                new Expression("Tags", "Contains", "test"), // Type function ( List.Contains )

                new Expression("Tags", "NOT Contains", "test1"), // Type function with not
                new Expression("Bad", "NOT Contains", "test"), // Not existing tag, should be ignored
                new Expression("FolderPath", "Not Contains", "test") // Null source value
            };
            dto.ExpressionSets = new List<ExpressionSet> { es };

            var tags = new List<string>() { "This", "is", "a", "test" };
            var operand = new Operand("Foo") { DateCreated = 100, Tags = tags, FolderPath = null };

            foreach (var rule in es.Expressions)
            {
                var result = rule.Execute(operand);
                if (rule.MemberName == "Bad")
                    Assert.False(result);
                else
                    Assert.True(result);
            }

        }
    }
}
