using Common.Core.Patterns.Caching.LRU;
using System;
using System.Linq;
using Xunit;

namespace Common.Core.Patterns.Tests.Caching.LRU
{
    public class FixedCapacityOptimizedLRUCacheTest
    {
        const int PreferredCapacity = 5;
        readonly FixedCapacityOptimizedLRUCache<int, int> subject;

        int head;
        int tail;
        int[] prev;
        int[] next;
        int[] values;

        public FixedCapacityOptimizedLRUCacheTest()
        {
            subject = new FixedCapacityOptimizedLRUCache<int, int>(PreferredCapacity);
        }

        private void LoadInternalPointers()
        {
            head = TestHelper.Helper.GetFieldValueFromInstance<int>("head", false, false, subject);
            tail = TestHelper.Helper.GetFieldValueFromInstance<int>("tail", false, false, subject);
            prev = TestHelper.Helper.GetFieldValueFromInstance<int[]>("prev", false, false, subject);
            next = TestHelper.Helper.GetFieldValueFromInstance<int[]>("next", false, false, subject);
            values = TestHelper.Helper.GetFieldValueFromInstance<int[]>("values", false, false, subject);
        }

        [Theory]
        [InlineData(500001)]
        [InlineData(-1)]
        public void Ctor_ShouldThrowArgumentOutOfRangeException_When_PreferredCapacityOutOfRange(int preferredCapacity)
           => Assert.Throws<ArgumentOutOfRangeException>(() => new FixedCapacityOptimizedLRUCache<int, int>(preferredCapacity));

        [Fact]
        public void Ctor_ShouldSetFixedCapacityAndInitializeCollection_When_Called()
        {
            Assert.Equal(0, subject.Size);
            Assert.Equal(PreferredCapacity, subject.Capacity);
        }

        [Fact]
        public void Set_ShouldSetBothTheHeadAndTailNodes_When_MapIsEmpty()
        {
            subject.Set(1, 10);

            LoadInternalPointers();

            Assert.Equal(head, tail);
            Assert.Equal(next, prev);
            Assert.Equal(1, subject.Size);
        }

        [Fact]
        public void Set_ShouldSetHeadToNewItem_When_ExistingHeadIsPresent()
        {
            subject.Set(1, 10);
            subject.Set(2, 20);

            LoadInternalPointers();

            Assert.NotEqual(head, tail);
            Assert.Equal(1, next[0]);
            Assert.Equal(0, prev[1]);
            Assert.Equal(2, subject.Size);
        }

        [Fact]
        public void Set_ShouldPromote_A_NodeToHead_When_ExistingKeyIsPresent()
        {
            subject.Set(1, 10);
            subject.Set(2, 20);
            subject.Set(1, 10);

            LoadInternalPointers();

            Assert.Equal(0, head);
            Assert.Equal(1, tail);
            Assert.Equal(10, values[head]);
            Assert.Equal(20, values[tail]);
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

            Assert.Equal(5, subject.Size);

            int[] expectedItemOrder = new int[] { 60, 10, 6, 5, 20 };
            int currentIndice = head;

            for (int i = 0; i < subject.Size; i++)
            {
                Assert.Equal(expectedItemOrder[i], values[currentIndice]);
                currentIndice = prev[currentIndice];
            }
        }

        [Fact]
        public void Set_ShouldNeverGoOverCapacity_Even_When_ManyItemsAreAdded()
        {
            int[] items = { 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

            foreach (int item in items)
            {
                subject.Set(item, item);
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

            int currentIndice = head;

            for (int i = 0; i < subject.Size; i++)
            {
                Assert.Equal(expectedItemOrder[i], values[currentIndice]);
                currentIndice = prev[currentIndice];
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

            Assert.Equal(10, values[tail]);
            Assert.Equal(20, values[head]);

            subject.Get(1);

            LoadInternalPointers();

            Assert.Equal(10, values[head]);
            Assert.Equal(20, values[tail]);
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
            int currentIndice = head;

            for (int i = 0; i < subject.Size; i++)
            {
                Assert.Equal(expectedItemOrder[i], values[currentIndice]);
                currentIndice = prev[currentIndice];
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

        [Theory]
        [InlineData(
            new int[] { 20, 10, 70, 90, 20 },
            new int[] { 3, 0, 1, 2 },
            new int[] { 0, 2, 3, 0 })]
        public void InternalMappers_ShouldShowAConsistentPattern_When_ItemsAreAdded(int[] items, int[] prev, int[] next)
        {
            foreach (int item in items)
            {
                subject.Set(item, item);
            }

            LoadInternalPointers();

            int nextIndice = head;
            for (int i = 0; i < subject.Size; i++)
            {
                Assert.Equal(this.prev[nextIndice], prev[nextIndice]);
                nextIndice = this.prev[nextIndice];
            }

            nextIndice = tail;
            for (int i = 0; i < subject.Size; i++)
            {
                Assert.Equal(this.next[nextIndice], next[nextIndice]);
                nextIndice = this.next[nextIndice];
            }
        }
    }
}
