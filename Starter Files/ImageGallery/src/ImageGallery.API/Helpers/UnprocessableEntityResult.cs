using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace ImageGallery.API.Helpers
{
    // QQHQ :: Is now part of Microsoft.AspNetCore.All 2.2.0+
    //public class UnprocessableEntityObjectResult : ObjectResult
    //{
    //    public UnprocessableEntityObjectResult(ModelStateDictionary modelState)
    //        : base(new SerializableError(modelState))
    //    {
    //        if (modelState == null)
    //        {
    //            throw new ArgumentNullException(nameof(modelState));
    //        }

    //        this.StatusCode = 422;
    //    }
    //}
}
