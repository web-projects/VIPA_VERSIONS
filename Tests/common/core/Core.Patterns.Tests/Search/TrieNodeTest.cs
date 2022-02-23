using Common.Core.Patterns.Search;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Common.Core.Patterns.Tests.Search
{
    public class TrieNodeTest
    {
        readonly TrieNode subject;

        public TrieNodeTest()
        {
            subject = new TrieNode(default, default, null);
        }

        [Fact]
        public void IsLeaf_Should_ReturnTrue_When_IsLeaf()
        {
            bool actual = subject.IsLeaf();

            Assert.True(actual);
        }

        [Fact]
        public void IsLeaf_Should_ReturnFalse_When_IsBranch()
        {
            subject.ChildrenMap.Add('a', new TrieNode('a', 1, subject));

            bool actual = subject.IsLeaf();

            Assert.False(actual);
        }

        [Fact]
        public void FindChildNode_Should_ReturnNull_When_KeyNotFound()
        {
            TrieNode actual = subject.FindChildNode('a');

            Assert.Null(actual);
        }

        [Fact]
        public void FindChildNode_Should_ReturnNode_When_KeyFound()
        {
            subject.ChildrenMap.Add('a', new TrieNode('a', 1, subject));

            TrieNode actual = subject.FindChildNode('a');

            Assert.NotNull(actual);
            Assert.Equal('a', actual.Value);
            Assert.Equal(1, actual.Depth);
            Assert.Same(subject, actual.Parent);
        }

        [Fact]
        public void DeleteChildNode_Should_DoNothing_When_KeyNotFound()
        {
            subject.DeleteChildNode('a');

            Assert.NotNull(subject);
        }

        [Fact]
        public void DeleteChildNode_Should_DeleteNode_When_KeyFound()
        {
            var expected = new TrieNode('a', 1, subject);
            subject.ChildrenMap.Add('a', expected);

            subject.DeleteChildNode('a');

            Assert.True(subject.IsLeaf());
        }
    }
}
