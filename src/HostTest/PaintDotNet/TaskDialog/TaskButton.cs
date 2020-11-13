/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////

using System.Drawing;

namespace HostTest
{
    internal sealed class TaskButton
    {
        private Image image;
        private string actionText;
        private string explanationText;

        public Image Image
        {
            get
            {
                return this.image;
            }
        }

        public string ActionText
        {
            get
            {
                return this.actionText;
            }
        }

        public string ExplanationText
        {
            get
            {
                return this.explanationText;
            }
        }

        public TaskButton(Image image, string actionText, string explanationText)
        {
            this.image = image;
            this.actionText = actionText;
            this.explanationText = explanationText;
        }
    }
}
