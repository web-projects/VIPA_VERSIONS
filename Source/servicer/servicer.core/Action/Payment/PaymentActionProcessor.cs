using Servicer.Core.CardType;
using Servicer.Core.EMVKernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using XO.Enums;

namespace Servicer.Core.Action.Payment
{
    public class PaymentActionProcessor
    {
        private string EMVKernelVersion;
        private string ContactlessKernelInformation;
        private List<AidKernelVersions> aidKernelVersions = new List<AidKernelVersions>();

        public PaymentActionProcessor(string eMVKernelVersion, string clessKernelVersion)
        {
            EMVKernelVersion = eMVKernelVersion;
            ContactlessKernelInformation = clessKernelVersion;

            // Map to EMV Kernel Versions
            string[] kernelRevisions = ContactlessKernelInformation.Split(';');
            foreach (string kernelVersion in kernelRevisions)
            {
                Debug.WriteLine($"EMV KERNEL VERSION: \"{kernelVersion}\"");
                SetContactlessEMVKernelVersion(kernelVersion.Substring(0, 2), kernelVersion.Substring(2));
            }
        }

        private string AddAidKernelVersion(string[] aids, string kernelVersion)
        {
            foreach (string aid in aids)
            {
                aidKernelVersions.Add(new AidKernelVersions(aid, kernelVersion));
            }
            return string.Empty;
        }

        private string SetContactlessEMVKernelVersion(string kernelIdentifier, string kernelVersion) => kernelIdentifier switch
        {
            "AK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.Where(x => x.CardBrand == TenderType.AMEX).Select(x => x.AIDValue).ToArray(), kernelVersion),
            "DK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.Where(x => x.CardBrand == TenderType.Discover).Select(x => x.AIDValue).ToArray(), kernelVersion),
            "IK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.Where(x => x.CardBrand == TenderType.Interac).Select(x => x.AIDValue).ToArray(), kernelVersion),
            "JK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.Where(x => x.CardBrand == TenderType.JCB || x.CardBrand == TenderType.Discover).Select(x => x.AIDValue).ToArray(), kernelVersion),
            "MK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.Where(x => x.CardBrand == TenderType.MasterCard).Select(x => x.AIDValue).ToArray(), kernelVersion),
            "VK" => AddAidKernelVersion(AidList.FirstDataRapidConnectAIDList.Where(x => x.CardBrand == TenderType.Visa).Select(x => x.AIDValue).ToArray(), kernelVersion),

            // UNMAPPED AIDS
            "CK" => AddAidKernelVersion(new string[] { kernelIdentifier }, kernelVersion),
            "EK" => AddAidKernelVersion(new string[] { kernelIdentifier }, kernelVersion),
            "EP" => AddAidKernelVersion(new string[] { kernelIdentifier }, kernelVersion),
            "GK" => AddAidKernelVersion(new string[] { kernelIdentifier }, kernelVersion),
            "MR" => AddAidKernelVersion(new string[] { kernelIdentifier }, kernelVersion),
            "PB" => AddAidKernelVersion(new string[] { kernelIdentifier }, kernelVersion),
            "PK" => AddAidKernelVersion(new string[] { kernelIdentifier }, kernelVersion),
            "RK" => AddAidKernelVersion(new string[] { kernelIdentifier }, kernelVersion),

            _ => throw new Exception($"Invalid EMV Kernel Identifier: '{kernelIdentifier}'.")
        };

        private AidEntry GetPaymentType(string paymentAID)
        {
            if (string.IsNullOrWhiteSpace(paymentAID))
            {
                return null;
            }
            //TODO: currently, only processor is FDRC - need to enhance to others
            AidEntry targetAid = AidList.FirstDataRapidConnectAIDList.FirstOrDefault(x => x.AIDValue.Equals(paymentAID, StringComparison.OrdinalIgnoreCase));
            return targetAid;
        }

        public string GetPaymentContactlessEMVKernelVersion(string applicationIdentifier)
        {
            //AidEntry aidType = GetPaymentType(applicationIdentifier);
            return aidKernelVersions.FirstOrDefault(x => x.AidValue == applicationIdentifier)?.KernelVersion ?? "NOT FOUND";
        }
    }
}
