﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>Tests for ProjectRootElementCache</summary>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Build.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Collections;
using Microsoft.Build.Framework;
using System.Collections;
using System;
using Microsoft.Build.Construction;
using System.IO;
using Microsoft.Build.Shared;

namespace Microsoft.Build.UnitTests.OM.Evaluation
{
    /// <summary>
    /// Tests for ProjectRootElementCache
    /// </summary>
    [TestClass]
    public class ProjectRootElementCache_Tests
    {
        /// <summary>
        /// Set up the test
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            // Empty the cache
            ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
            GC.Collect();
        }

        /// <summary>
        /// Tear down the test
        /// </summary>
        [TestCleanup]
        public void TearDown()
        {
            // Empty the cache
            ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
            GC.Collect();
        }

        /// <summary>
        /// Verifies that a null entry fails
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InternalErrorException))]
        public void AddNull()
        {
            ProjectCollection.GlobalProjectCollection.ProjectRootElementCache.Get("c:\\foo", (p, c) => null, true);
        }

        /// <summary>
        /// Verifies that the delegate cannot return a project with a different path
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InternalErrorException))]
        public void AddUnsavedProject()
        {
            ProjectCollection.GlobalProjectCollection.ProjectRootElementCache.Get("c:\\foo", (p, c) => ProjectRootElement.Create("c:\\bar"), true);
        }

        /// <summary>
        /// Tests that an entry added to the cache can be retrieved.
        /// </summary>
        [TestMethod]
        public void AddEntry()
        {
            ProjectRootElement projectRootElement = ProjectRootElement.Create("c:\\foo");
            ProjectRootElement projectRootElement2 = ProjectCollection.GlobalProjectCollection.ProjectRootElementCache.Get("c:\\foo", (p, c) => { throw new InvalidOperationException(); }, true);

            Assert.AreSame(projectRootElement, projectRootElement2);
        }

        /// <summary>
        /// Tests that a strong reference is held to a single item
        /// </summary>
        [TestMethod]
        public void AddEntryStrongReference()
        {
            ProjectRootElement projectRootElement = ProjectRootElement.Create("c:\\foo");

            projectRootElement = null;
            GC.Collect();

            projectRootElement = ProjectCollection.GlobalProjectCollection.ProjectRootElementCache.Get("c:\\foo", (p, c) => { throw new InvalidOperationException(); }, true);

            Assert.IsNotNull(projectRootElement);

            ProjectCollection.GlobalProjectCollection.ProjectRootElementCache.DiscardStrongReferences();
            projectRootElement = null;
            GC.Collect();

            Assert.IsNull(ProjectCollection.GlobalProjectCollection.ProjectRootElementCache.TryGet("c:\\foo"));
        }

        /// <summary>
        /// Cache should not return a ProjectRootElement if the file it was loaded from has since changed -
        /// if the cache was configured to auto-reload.
        /// </summary>
        [TestMethod]
        public void GetProjectRootElementChangedOnDisk1()
        {
            string path = null;

            try
            {
                ProjectRootElementCache cache = new ProjectRootElementCache(true /* auto reload from disk */);

                path = FileUtilities.GetTemporaryFile();

                ProjectRootElement xml0 = ProjectRootElement.Create(path);
                xml0.Save();

                cache.AddEntry(xml0);

                ProjectRootElement xml1 = cache.TryGet(path);
                Assert.AreEqual(true, Object.ReferenceEquals(xml0, xml1));

                File.SetLastWriteTime(path, DateTime.Now + new TimeSpan(1, 0, 0));

                ProjectRootElement xml2 = cache.TryGet(path);
                Assert.AreEqual(false, Object.ReferenceEquals(xml0, xml2));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Cache should return a ProjectRootElement directly even if the file it was loaded from has since changed -
        /// if the cache was configured to NOT auto-reload.
        /// </summary>
        [TestMethod]
        public void GetProjectRootElementChangedOnDisk2()
        {
            string path = null;

            try
            {
                ProjectRootElementCache cache = new ProjectRootElementCache(false /* do not auto reload from disk */);

                path = FileUtilities.GetTemporaryFile();

                ProjectRootElement xml0 = ProjectRootElement.Create(path);
                xml0.Save();

                cache.AddEntry(xml0);

                ProjectRootElement xml1 = cache.TryGet(path);
                Assert.AreEqual(true, Object.ReferenceEquals(xml0, xml1));

                File.SetLastWriteTime(path, DateTime.Now + new TimeSpan(1, 0, 0));

                ProjectRootElement xml2 = cache.TryGet(path);
                Assert.AreEqual(true, Object.ReferenceEquals(xml0, xml2));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
