using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;
using System.ComponentModel;

namespace PoshWpf
{
   [RunInstaller(true)]
   public class PoshWpfSnapIn : PSSnapIn
   {
      public override string Name
      {
         get { return "PoshWpf"; }
      }
      public override string Vendor
      {
         get { return ""; }
      }
      public override string VendorResource
      {
         get { return "PoshWpf,"; }
      }
      public override string Description
      {
         get { return "Registers the CmdLets and Providers in this assembly"; }
      }
      public override string DescriptionResource
      {
         get { return "PoshWpf,Registers the CmdLets and Providers in this assembly"; }
      }
   }
}
