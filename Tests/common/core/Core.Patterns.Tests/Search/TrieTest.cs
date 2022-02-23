using Common.Core.Patterns.Search;
using System;
using Xunit;

namespace Common.Core.Patterns.Tests.Search
{
    public class TrieTest
    {
        readonly Trie subject;

        public TrieTest()
        {
            subject = new Trie();
        }

        [Fact]
        public void Prefix_Should_ReturnRootNode_When_PrefixNotFound()
        {
            TrieNode actual = subject.Prefix("a");

            Assert.NotNull(actual);
            Assert.Equal('^', actual.Value);
            Assert.Equal(0, actual.Depth);
            Assert.Null(actual.Parent);
        }

        [Fact]
        public void Prefix_Should_ReturnPrefix_When_PrefixFound()
        {
            subject.Insert("ab");

            TrieNode actual = subject.Prefix("a");

            Assert.NotNull(actual);
            Assert.Equal('a', actual.Value);
            Assert.Equal(1, actual.Depth);
            Assert.NotNull(actual.Parent);
        }

        [Fact]
        public void Search_Should_ReturnFalse_When_StringNotFound()
        {
            subject.Insert("ab");

            bool actual = subject.Search("cd");

            Assert.False(actual);
        }

        [Theory]
        [InlineData(false, "ab", "ab")]
        [InlineData(false, "ab", "AB")]
        [InlineData(false, "AB", "ab")]
        [InlineData(false, "AB", "AB")]
        [InlineData(true, "ab", "ab")]
        [InlineData(true, "ab", "AB")]
        [InlineData(true, "AB", "ab")]
        [InlineData(true, "AB", "AB")]
        public void Search_Should_ReturnExpectedResult_When_IgnoreCaseSpecified(bool ignoreCase, string insertString, string searchString)
        {
            bool expected = ignoreCase || insertString.Equals(searchString, StringComparison.Ordinal);
            subject.IgnoreCase = ignoreCase;
            subject.Insert(insertString);

            bool actual = subject.Search(searchString);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SearchReadOnlySpan_Should_ReturnFalse_When_StringNotFound()
        {
            subject.Insert("ab");
            ReadOnlySpan<char> searchSpan = "cd".AsSpan();

            bool actual = subject.Search(searchSpan);

            Assert.False(actual);
        }

        [Theory]
        [InlineData(false, "ab", "ab")]
        [InlineData(false, "ab", "AB")]
        [InlineData(false, "AB", "ab")]
        [InlineData(false, "AB", "AB")]
        [InlineData(true, "ab", "ab")]
        [InlineData(true, "ab", "AB")]
        [InlineData(true, "AB", "ab")]
        [InlineData(true, "AB", "AB")]
        public void SearchReadOnlySpan_Should_ReturnExpectedResult_When_IgnoreCaseSpecified(bool ignoreCase, string insertString, string searchString)
        {
            bool expected = ignoreCase || insertString.Equals(searchString, StringComparison.Ordinal);
            subject.IgnoreCase = ignoreCase;
            subject.Insert(insertString);

            bool actual = subject.Search(searchString.AsSpan());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Delete_Should_DoNothing_When_StringNotFound()
        {
            subject.Insert("ab");

            subject.Delete("cd");

            Assert.True(subject.Search("ab"));
        }

        [Theory]
        [InlineData(false, "ab", "ab")]
        [InlineData(false, "ab", "AB")]
        [InlineData(false, "AB", "ab")]
        [InlineData(false, "AB", "AB")]
        [InlineData(true, "ab", "ab")]
        [InlineData(true, "ab", "AB")]
        [InlineData(true, "AB", "ab")]
        [InlineData(true, "AB", "AB")]
        public void Delete_Should_DeleteNodes_When_IgnoreCaseSpecified(bool ignoreCase, string insertString, string searchString)
        {
            bool expectedDelete = ignoreCase || insertString.Equals(searchString, StringComparison.Ordinal);
            subject.IgnoreCase = ignoreCase;
            subject.Insert(insertString);

            subject.Delete(searchString);

            if (expectedDelete)
                Assert.False(subject.Search(insertString));
            else
                Assert.True(subject.Search(insertString));
        }
    }
}
