using Servicer.Core.Action.Payment;
using Servicer.Core.EMVKernel;
using System.Collections.Generic;
using Xunit;

namespace Servicer.Core.Tests.Action.Payment
{
    public class PaymentActionProcessorTests
    {
        const int ContactLessKernelApps = 14;
        readonly string eMVKernelVersion = "1.2.3";
        readonly string ContactlessKernelInformation = "AK1.0.1;DK2.0.2;IK3.0.3;JK4.0.4;MK5.0.5;VK6.0.6";

        readonly PaymentActionProcessor subject;

        public PaymentActionProcessorTests()
        {
            subject = new PaymentActionProcessor(eMVKernelVersion, ContactlessKernelInformation);
        }

        [Fact]
        public void PaymentActionProcessor_DefaultEntriesMatch()
        {
            List<AidKernelVersions> aidKernelVersions = TestHelper.Helper.GetFieldValueFromInstance<List<AidKernelVersions>>("aidKernelVersions", false, false, subject);
            Assert.Equal(ContactLessKernelApps, aidKernelVersions.Count);
        }

        [Theory]
        [InlineData("A00000002501", "1.0.1")]       // AMEX
        [InlineData("A0000001523010", "2.0.2")]     // DinersClub and Discover
        [InlineData("A0000001524010", "2.0.2")]     // Discover Common Debit
        [InlineData("A0000002771010", "3.0.3")]     // Interac
        [InlineData("A0000000651010", "4.0.4")]     // JCB
        [InlineData("A0000000043060", "5.0.5")]     // MasterCard International Maestro
        [InlineData("A0000000042203", "5.0.5")]     // MasterCard U.S. Maestro common
        [InlineData("A0000000041010", "5.0.5")]     // MasterCard Credit
        [InlineData("A0000000980840", "6.0.6")]     // Visa Common
        [InlineData("A0000000033010", "6.0.6")]     // Visa Interlink
        [InlineData("A0000000032010", "6.0.6")]     // Visa Electron
        [InlineData("A0000000031010", "6.0.6")]     // Visa Credit and Visa Debit International
        //[InlineData("A0000006200620", "")]        // DNA US Common Debit
        public void GetPaymentContactlessEMVKernelVersion_ReturnsAppropriateKernelVersion_ForAIDProvided(string applicationIdentifier, string expectedKernelVersion)
        {
            string actualKernelVersion = subject.GetPaymentContactlessEMVKernelVersion(applicationIdentifier);
            Assert.Equal(expectedKernelVersion, actualKernelVersion);
        }
    }
}
