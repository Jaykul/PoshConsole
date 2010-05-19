using System;
using System.Linq;
using System.Management.Automation;
using System.Windows;
using System.Windows.Markup;
using System.Xaml;

namespace PoshWpf
{
   [MarkupExtensionReturnType(typeof(RoutedEventHandler))]
   public class PoshExtension : MarkupExtension
   {
      public string Code { get; set; }

      public PoshExtension(string code)
      {
         Code = code;
      }

      public override object ProvideValue(IServiceProvider serviceProvider)
      {
         var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
         var root = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
         var variables = new[]
         {
             new PSVariable("this",(target != null) ? target.TargetObject : ((root != null) ? root.RootObject : null)),
             new PSVariable("window", (root != null) ? root.RootObject : null),
             new PSVariable("PowerBootsModule", Invoker.Module)
         };
         // Invoker.SetScriptVariableValue("PowerBootsModule", Invoker.Module);

         var collection = Invoker.Invoke("[system.windows.routedeventhandler]{Invoke-BootsWindow $window {" + Code + " @args} @args }", variables);
         return collection.First().BaseObject as RoutedEventHandler;
      }
   }
}
