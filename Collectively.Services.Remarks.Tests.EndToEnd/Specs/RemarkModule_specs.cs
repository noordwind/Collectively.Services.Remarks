using System;
using System.Collections.Generic;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Tests.EndToEnd.Framework;
using System.Linq;
using Machine.Specifications;

namespace Collectively.Services.Remarks.Tests.EndToEnd.Specs
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

        protected static IEnumerable<Remark> FetchRemarks(int page = 1, int results = 1000)
            => HttpClient.GetAsync<IEnumerable<Remark>>($"remarks?results={results}&page={page}").WaitForResult();

        protected static IEnumerable<Category> FetchCategories()
            => HttpClient.GetAsync<IEnumerable<Category>>("remarks/categories").WaitForResult();

        protected static Remark FetchRemark(Guid id)
            => HttpClient.GetAsync<Remark>($"remarks/{id}").WaitForResult();
    }

    [Subject("RemarkService fetch remarks")]
    public class when_fetching_remarks : RemarkModule_specs
    {
        protected static int Results = 25;

        Because of = () => Remarks = FetchRemarks(results: Results);

        It should_not_be_null = () => Remarks.ShouldNotBeNull();

        It should_not_be_empty = () => Remarks.ShouldNotBeEmpty();

        It should_return_not_more_than_requested_amount_of_data = ()
            => Remarks.Count().ShouldBeLessThanOrEqualTo(Results);
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