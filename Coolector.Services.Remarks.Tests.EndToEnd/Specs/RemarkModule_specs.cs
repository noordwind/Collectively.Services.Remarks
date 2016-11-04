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

        protected static IEnumerable<Remark> GetRemarksWithCategory(string categoryName)
            => HttpClient.GetCollectionAsync<Remark>($"remarks?radius=10000&longitude=1.0&latitude=1.0&categories={categoryName}").WaitForResult();

        protected static IEnumerable<Remark> GetRemarksWithState(string state)
            => HttpClient.GetCollectionAsync<Remark>($"remarks?radius=10000&longitude=1.0&latitude=1.0&state={state}").WaitForResult();

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

    [Subject("RemarkService fetch remarks with category")]
    public class when_fetching_remarks_with_specific_category : RemarkModule_specs
    {
        protected static string Category = "damages";

        Because of = () => Remarks = GetRemarksWithCategory(Category);

        It should_return_non_empty_collection = () =>
        {
            Remarks.ShouldNotBeEmpty();
            foreach (var remark in Remarks)
            {
                remark.Id.ShouldNotEqual(Guid.Empty);
                remark.Author.UserId.ShouldNotBeEmpty();
                remark.Author.Name.ShouldNotBeEmpty();
                remark.Category.Id.ShouldNotEqual(Guid.Empty);
                remark.Category.Name.ShouldNotBeEmpty();
                remark.Location.Coordinates.Length.ShouldEqual(2);
                remark.Location.Coordinates[0].ShouldNotEqual(0);
                remark.Location.Coordinates[1].ShouldNotEqual(0);
            }
        };

        It should_contain_remarks_with_the_same_category = ()
            => Remarks.All(x => x.Category.Name == Category).ShouldBeTrue();
    }

    [Subject("RemarkService fetch resolved remarks")]
    public class when_fetching_resolved_remarks : RemarkModule_specs
    {
        protected static string State = "resolved";

        Because of = () => Remarks = GetRemarksWithState(State);

        It should_return_non_empty_collection = () =>
        {
            Remarks.ShouldNotBeEmpty();
            foreach (var remark in Remarks)
            {
                remark.Id.ShouldNotEqual(Guid.Empty);
                remark.Author.UserId.ShouldNotBeEmpty();
                remark.Author.Name.ShouldNotBeEmpty();
                remark.Category.Id.ShouldNotEqual(Guid.Empty);
                remark.Category.Name.ShouldNotBeEmpty();
                remark.Location.Coordinates.Length.ShouldEqual(2);
                remark.Location.Coordinates[0].ShouldNotEqual(0);
                remark.Location.Coordinates[1].ShouldNotEqual(0);
            }
        };

        It should_contain_only_resolved_remarks = ()
            => Remarks.All(x => x.Resolved).ShouldBeTrue();
    }

    [Subject("RemarkService fetch active remarks")]
    public class when_fetching_remarks_with_all_states : RemarkModule_specs
    {
        protected static string State = "active";

        Because of = () => Remarks = GetRemarksWithState(State);

        It should_return_non_empty_collection = () =>
        {
            Remarks.ShouldNotBeEmpty();
            foreach (var remark in Remarks)
            {
                remark.Id.ShouldNotEqual(Guid.Empty);
                remark.Author.UserId.ShouldNotBeEmpty();
                remark.Author.Name.ShouldNotBeEmpty();
                remark.Category.Id.ShouldNotEqual(Guid.Empty);
                remark.Category.Name.ShouldNotBeEmpty();
                remark.Location.Coordinates.Length.ShouldEqual(2);
                remark.Location.Coordinates[0].ShouldNotEqual(0);
                remark.Location.Coordinates[1].ShouldNotEqual(0);
            }
        };

        It should_return_active_remarks = ()
            => Remarks.All(x => x.Resolved == false).ShouldBeTrue();
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