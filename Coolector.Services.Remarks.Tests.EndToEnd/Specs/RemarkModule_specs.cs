using System;
using System.Collections.Generic;
using System.IO;
using Coolector.Services.Remarks.Domain;
using Coolector.Services.Remarks.Tests.EndToEnd.Framework;
using System.Linq;
using Machine.Specifications;
using FluentAssertions;

namespace Coolector.Services.Remarks.Tests.EndToEnd.Specs
{
    public abstract class RemarkModule_specs
    {
        protected static IHttpClient HttpClient = new CustomHttpClient("http://localhost:10002");
        protected static Remark Remark;
        protected static IEnumerable<Remark> Remarks;
        protected static IEnumerable<Category> Categories;
        protected static Guid RemarkId;

        protected static void InitializeAndFetch()
        {
            var remark = FetchRemarks().First();
            RemarkId = remark.Id;
        }

        protected static IEnumerable<Remark> FetchRemarks()
            => HttpClient.GetAsync<IEnumerable<Remark>>("remarks?latest=true").WaitForResult();

        protected static IEnumerable<Remark> FetchNearestRemarks()
            => HttpClient.GetCollectionAsync<Remark>("remarks?results=100&radius=10000&longitude=1.0&latitude=1.0&nearest=true").WaitForResult();

        protected static IEnumerable<Category> FetchCategories()
            => HttpClient.GetAsync<IEnumerable<Category>>("remarks/categories").WaitForResult();

        protected static Remark FetchRemark(Guid id)
            => HttpClient.GetAsync<Remark>($"remarks/{id}").WaitForResult();
    }

    [Subject("RemarkService fetch remarks")]
    public class when_fetching_remarks : RemarkModule_specs
    {
        Because of = () => Remarks = FetchRemarks();

        It should_not_be_null = () => Remarks.ShouldNotBeNull();

        It should_not_be_empty = () => Remarks.ShouldNotBeEmpty();
    }

    [Subject("RemarkService fetch nearest remarks")]
    public class when_fetching_nearest_remarks : RemarkModule_specs
    {
        Because of = () => Remarks = FetchNearestRemarks();

        It should_not_be_null = () => Remarks.ShouldNotBeNull();

        It should_not_be_empty = () => Remarks.ShouldNotBeEmpty();

        It should_return_remarks_in_correct_order = () =>
        {
            Remark previousRemark = null;
            foreach (var remark in Remarks)
            {
                if (previousRemark != null)
                {
                    previousRemark.Location.Latitude.ShouldBeLessThanOrEqualTo(remark.Location.Latitude);
                    previousRemark.Location.Longitude.ShouldBeLessThanOrEqualTo(remark.Location.Longitude);
                }
                previousRemark = remark;
            }
        };
    }

    [Subject("RemarkService fetch single remark")]
    public class when_fetching_single_remark : RemarkModule_specs
    {
        Establish context = () => InitializeAndFetch();

        Because of = () => Remark = FetchRemark(RemarkId);

        It should_not_be_null = () => Remark.ShouldNotBeNull();

        It should_have_correct_id = () => Remark.Id.ShouldEqual(RemarkId);

        It should_return_remark = () =>
        {
            Remark.Id.ShouldNotEqual(Guid.Empty);
            Remark.Author.ShouldNotBeNull();
            Remark.Category.ShouldNotBeNull();
            Remark.CreatedAt.ShouldNotEqual(default(DateTime));
            Remark.Description.ShouldNotBeEmpty();
            Remark.Location.ShouldNotBeNull();
        };
    }

    [Subject("RemarkService fetch categories")]
    public class when_fetching_categories : RemarkModule_specs
    {
        Because of = () => Categories = FetchCategories();

        It should_not_be_null = () => Categories.ShouldNotBeNull();

        It should_not_be_empty = () => Categories.ShouldNotBeEmpty();
    }
}