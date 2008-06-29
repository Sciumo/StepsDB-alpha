﻿
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

// no-inlining seems to be broken on x64
// https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=162364&wa=wsignin1.0

namespace Bend {
    public class WhoCalls
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string WhatsMyName()
        {
            StackFrame stackFrame = new StackFrame();
            MethodBase methodBase = stackFrame.GetMethod();
            return methodBase.Name;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string WhoCalledMe()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame stackFrame = stackTrace.GetFrame(1);
            MethodBase methodBase = stackFrame.GetMethod();
            
            return methodBase.Name;
        }


    }
}

namespace BendTests
{
    using Bend;
    using NUnit.Framework;

    [TestFixture]
    public class ZZ_TODO_WhoCalls
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string TestWhatsMyName() {
            return WhoCalls.WhatsMyName();
        }
        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public string TestWhoCalledMe() {
        //    return WhoCalls.WhoCalledMe();
        //}

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void T00_WhoCalls()
        {
            Assert.AreEqual("TestWhatsMyName", TestWhatsMyName(), "1");

            //Assert.AreEqual("T00_WhoCalls", TestWhoCalledMe(), "2");
            
            
            
        }
    }
}