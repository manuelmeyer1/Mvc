// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc.Razor.Directives;
using Microsoft.AspNetCore.Mvc.Razor.Host.Internal;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class MvcCSharpCodeGenerator : CSharpCodeGenerator
    {
        private readonly GeneratedTagHelperAttributeContext _tagHelperAttributeContext;
        private readonly TagHelperChunkDecorator _tagHelperChunkDecorator;
        private readonly string _defaultModel;
        private readonly string _injectAttribute;

        public MvcCSharpCodeGenerator(
            CodeGeneratorContext context,
            string defaultModel,
            string injectAttribute,
            GeneratedTagHelperAttributeContext tagHelperAttributeContext)
            : base(context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (defaultModel == null)
            {
                throw new ArgumentNullException(nameof(defaultModel));
            }

            if (injectAttribute == null)
            {
                throw new ArgumentNullException(nameof(injectAttribute));
            }

            if (tagHelperAttributeContext == null)
            {
                throw new ArgumentNullException(nameof(tagHelperAttributeContext));
            }

            _tagHelperAttributeContext = tagHelperAttributeContext;
            _defaultModel = defaultModel;
            _injectAttribute = injectAttribute;
            _tagHelperChunkDecorator = new TagHelperChunkDecorator(Context);
        }

        public override CodeGeneratorResult Generate()
        {
            _tagHelperChunkDecorator.Accept(Context.ChunkTreeBuilder.Root.Children);
            return base.Generate();
        }

        protected override CSharpCodeWritingScope BuildClassDeclaration(CSharpCodeWriter writer)
        {
            if (Context.Host.DesignTimeMode &&
                string.Equals(
                    Path.GetFileName(Context.SourceFile),
                    ViewHierarchyUtility.ViewImportsFileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                // Write a using TModel = System.Object; token during design time to make intellisense work
                writer.WriteLine($"using {ChunkHelper.TModelToken} = {typeof(object).FullName};");
            }

            return base.BuildClassDeclaration(writer);
        }

        protected override void BuildAfterExecuteContent(CSharpCodeWriter writer, IList<Chunk> chunks)
        {
            new ViewComponentTagHelperChunkVisitor(writer, Context).Accept(chunks);
        }

        protected override CSharpCodeVisitor CreateCSharpCodeVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var csharpCodeVisitor = base.CreateCSharpCodeVisitor(writer, context);

            csharpCodeVisitor.TagHelperRenderer.AttributeValueCodeRenderer =
                new MvcTagHelperAttributeValueCodeRenderer(_tagHelperAttributeContext);

            return csharpCodeVisitor;
        }

        protected override CSharpDesignTimeCodeVisitor CreateCSharpDesignTimeCodeVisitor(
            CSharpCodeVisitor csharpCodeVisitor,
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
        {
            if (csharpCodeVisitor == null)
            {
                throw new ArgumentNullException(nameof(csharpCodeVisitor));
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new MvcCSharpDesignTimeCodeVisitor(csharpCodeVisitor, writer, context);
        }

        protected override void BuildConstructor(CSharpCodeWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            base.BuildConstructor(writer);

            writer.WriteLineHiddenDirective();

            var injectVisitor = new InjectChunkVisitor(writer, Context, _injectAttribute);
            injectVisitor.Accept(Context.ChunkTreeBuilder.Root.Children);

            writer.WriteLine();
            writer.WriteLineHiddenDirective();
        }
    }
}