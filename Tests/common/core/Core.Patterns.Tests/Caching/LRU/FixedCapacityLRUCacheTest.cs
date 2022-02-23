using Common.Core.Patterns.Caching.LRU;
using System;
using System.Collections.Concurrent;
using System.Linq;
using Xunit;
using static Common.Core.Patterns.Caching.LRU.FixedCapacityLRUCache<int, int>;

namespace Common.Core.Patterns.Tests.Caching.LRU
{
    public class FixedCapacityLRUCacheTest
    {
        const int PreferredCapacity = 10;
        readonly FixedCapacityLRUCache<int, int> subject;

        LRUNode<int, int> head;
        LRUNode<int, int> tail;
        ConcurrentDictionary<int, LRUNode<int, int>> itemMap;

        public FixedCapacityLRUCacheTest()
        {
            subject = new FixedCapacityLRUCache<int, int>(PreferredCapacity);
        }

        private void LoadInternalPointers()
        {
            itemMap = TestHelper.Helper.GetFieldValueFromInstance<ConcurrentDictionary<int, LRUNode<int, int>>>("itemMap", false, false, subject);
            head = TestHelper.Helper.GetFieldValueFromInstance<LRUNode<int, int>>("head", false, false, subject);
            tail = TestHelper.Helper.GetFieldValueFromInstance<LRUNode<int, int>>("tail", false, false, subject);
        }

        [Theory]
        [InlineData(500001)]
        [InlineData(-1)]
        public void Ctor_ShouldThrowArgumentOutOfRangeException_When_PreferredCapacityOutOfRange(int preferredCapacity)
            => Assert.Throws<ArgumentOutOfRangeException>(() => new FixedCapacityLRUCache<int, int>(preferredCapacity));

        [Fact]
        public void Ctor_ShouldSetFixedCapacityAndInitializeCollection_When_Called()
        {
            LoadInternalPointers();

            Assert.Equal(0, subject.Size);
            Assert.Equal(PreferredCapacity, subject.Capacity);
            Assert.NotNull(itemMap);
            Assert.Empty(itemMap);
        }

        [Fact]
        public void Set_ShouldSetBothTheHeadAndTailNodes_When_MapIsEmpty()
        {
            subject.Set(1, 10);

            LoadInternalPointers();

            Assert.Same(head, tail);
            Assert.Equal(1, subject.Size);
        }

        [Fact]
        public void Set_ShouldSetHeadToNewItem_When_ExistingHeadIsPresent()
        {
            subject.Set(1, 10);
            subject.Set(2, 20);

            LoadInternalPointers();

            Assert.NotSame(head, tail);
            Assert.Equal(20, head.value);
            Assert.Equal(10, tail.value);
            Assert.Equal(2, subject.Size);
        }

        [Fact]
        public void Set_ShouldPromote_A_NodeToHead_When_ExistingKeyIsPresent()
        {
            subject.Set(1, 10);
            subject.Set(2, 20);
            subject.Set(1, 10);

            LoadInternalPointers();

            Assert.NotSame(head, tail);
            Assert.Equal(10, head.value);
            Assert.Equal(20, tail.value);
        }

        [Fact]
        public void Set_ShouldAddItemsAsTheyArrive_And_PromoteAsNecessary_When_Called()
        {
            int[] items = new int[] { 10, 20, 5, 6, 10, 60 };

            foreach (int item in items)
            {
                subject.Set(item, item);
            }

            LoadInternalPointers();

            int[] expectedItemOrder = new int[] { 60, 10, 6, 5, 20 };
            int index = 0;

            Assert.Equal(expectedItemOrder.Length, itemMap.Count);

            LRUNode<int, int> node = head;
            while (node != null)
            {
                Assert.Equal(expectedItemOrder[index++], node.value);
                node = node.next;
            }
        }

        [Fact]
        public void Set_ShouldNeverGoOverCapacity_Even_When_ManyItemsAreAdded()
        {
            const int itemsToAdd = PreferredCapacity << 5;

            Random rnd = new Random();
            for (int i = 0; i < itemsToAdd; i++)
            {
                int rndValue = rnd.Next(1, itemsToAdd + 1);
                subject.Set(rndValue, rndValue);
            }

            Assert.Equal(subject.Capacity, subject.Size);
        }

        [Fact]
        public void Set_ShouldEnsureProperOrdering_Even_When_WeAreOverCapacity()
        {
            int[] items = { 10, 50, 40, 30, 5, 10, 20, 44, 25, 29, 90, 84, 33, 22, 5 };
            int[] expectedItemOrder = items.Reverse().Distinct().Take(PreferredCapacity).ToArray();

            foreach (int item in items)
            {
                subject.Set(item, item);
            }

            LoadInternalPointers();

            int index = 0;

            LRUNode<int, int> node = head;
            while (node != null)
            {
                Assert.Equal(expectedItemOrder[index++], node.value);
                node = node.next;
            }
        }

        [Fact]
        public void Get_ShouldReturnZero_When_TheKeyDoesNotExist()
        {
            int actualValue = subject.Get(1);
            Assert.Equal(0, actualValue);
        }

        [Fact]
        public void Get_ShouldReturnTheHeadValue_When_OnlyOneNodeExists()
        {
            subject.Set(1, 10);

            int actualValue = subject.Get(1);

            Assert.Equal(10, actualValue);
        }

        [Fact]
        public void Get_ShouldForceASwap_OfTheHeadAndTailNodes_When_TwoOrMoreItemsExist()
        {
            subject.Set(1, 10);
            subject.Set(2, 20);

            LoadInternalPointers();

            Assert.Equal(10, tail.value);
            Assert.Equal(20, head.value);

            subject.Get(1);

            LoadInternalPointers();

            Assert.Equal(10, head.value);
            Assert.Equal(20, tail.value);
        }

        [Fact]
        public void Get_ShouldTakeANonLeafNode_AndPromoteIt_When_Called()
        {
            subject.Set(1, 10);
            subject.Set(2, 20);
            subject.Set(3, 30);
            subject.Set(4, 40);

            subject.Get(2);

            LoadInternalPointers();

            int[] expectedItemOrder = { 20, 40, 30, 10 };
            int index = 0;

            LRUNode<int, int> node = head;
            while (node != null)
            {
                Assert.Equal(expectedItemOrder[index++], node.value);
                node = node.next;
            }
        }

        [Fact]
        public void Enumerator_ShouldBreak_When_NoItemsAreInCache()
        {
            int iterations = 0;

            foreach (var _ in subject)
            {
                iterations++;
            }

            Assert.Equal(0, iterations);
        }

        [Fact]
        public void Enumerator_ShouldIterateAllLRUItems_InOrder_When_Called()
        {
            subject.Set(1, 20);
            subject.Set(2, 10);
            subject.Set(3, 30);
            subject.Set(4, 50);
            subject.Set(5, 70);
            subject.Set(3, 90);
            subject.Set(1, 20);

            LoadInternalPointers();

            int[] expectedItemOrder = { 20, 90, 70, 50, 10 };
            int index = 0;

            foreach (var item in subject)
            {
                Assert.Equal(expectedItemOrder[index++], item);
            }
        }
    }
}
