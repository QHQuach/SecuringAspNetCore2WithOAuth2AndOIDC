using AutoMapper;
using ImageGallery.API.Helpers;
using ImageGallery.API.Services;
using ImageGallery.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace ImageGallery.API.Controllers
{
    [Authorize] // QQHQ :: API
    [Route("api/images")]
    public class ImagesController : Controller
    {
        private readonly IGalleryRepository _galleryRepository;
        private readonly IHostingEnvironment _hostingEnvironment;

        public ImagesController(IGalleryRepository galleryRepository,
            IHostingEnvironment hostingEnvironment)
        {
            this._galleryRepository = galleryRepository;
            this._hostingEnvironment = hostingEnvironment;
        }

        [HttpGet()]
        public IActionResult GetImages()
        {
            // QQHQ :: API
            string ownerId = this.User.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;

            // get from repo
            IEnumerable<Entities.Image> imagesFromRepo = String.IsNullOrWhiteSpace(ownerId) ?
                this._galleryRepository.GetImages() :
                this._galleryRepository.GetImages(ownerId);

            // map to model
            IEnumerable<Image> imagesToReturn = Mapper.Map<IEnumerable<Image>>(imagesFromRepo);

            // return
            return this.Ok(imagesToReturn);
        }

        // QQHQ :: POLEXT
        [Authorize(Policy = "MustOwnImage")]
        [HttpGet("{id}", Name = "GetImage")]
        public IActionResult GetImage(Guid id)
        {
            Entities.Image imageFromRepo = this._galleryRepository.GetImage(id);

            if (imageFromRepo == null)
            {
                return this.NotFound();
            }

            Image imageToReturn = Mapper.Map<Image>(imageFromRepo);

            return this.Ok(imageToReturn);
        }

        // QQHQ :: API
        [Authorize(Roles = "PayingUser")]
        [HttpPost()]
        public IActionResult CreateImage([FromBody] ImageForCreation imageForCreation)
        {
            if (imageForCreation == null)
            {
                return this.BadRequest();
            }

            if (!this.ModelState.IsValid)
            {
                // return 422 - Unprocessable Entity when validation fails
                return new UnprocessableEntityObjectResult(this.ModelState);
            }

            // Automapper maps only the Title in our configuration
            Entities.Image imageEntity = Mapper.Map<Entities.Image>(imageForCreation);

            // ownerId should be set - can't save image in starter solution, will
            // be fixed during the course
            //imageEntity.OwnerId = ...;

            // QQHQ :: API :: IDP Call???
            var ownerId = this.User.Claims.FirstOrDefault(claim => claim.Type == "sub").Value;
            imageEntity.OwnerId = ownerId;

            // Create an image from the passed-in bytes (Base64), and
            // set the filename on the image

            // get this environment's web root path (the path
            // from which static content, like an image, is served)
            string webRootPath = this._hostingEnvironment.WebRootPath;

            // create the filename
            string fileName = Guid.NewGuid().ToString() + ".jpg";

            // the full file path
            string filePath = Path.Combine($"{webRootPath}/images/{fileName}");

            // write bytes and auto-close stream
            System.IO.File.WriteAllBytes(filePath, imageForCreation.Bytes);

            // fill out the filename
            imageEntity.FileName = fileName;

            // add and save.
            this._galleryRepository.AddImage(imageEntity);

            if (!this._galleryRepository.Save())
            {
                throw new Exception($"Adding an image failed on save.");
            }

            Image imageToReturn = Mapper.Map<Image>(imageEntity);

            return this.CreatedAtRoute("GetImage",
                new { id = imageToReturn.Id },
                imageToReturn);
        }

        // QQHQ :: POLEXT
        [Authorize(Policy = "MustOwnImage")]
        [HttpDelete("{id}")]
        public IActionResult DeleteImage(Guid id)
        {

            Entities.Image imageFromRepo = this._galleryRepository.GetImage(id);

            if (imageFromRepo == null)
            {
                return this.NotFound();
            }

            this._galleryRepository.DeleteImage(imageFromRepo);

            if (!this._galleryRepository.Save())
            {
                throw new Exception($"Deleting image with {id} failed on save.");
            }

            return this.NoContent();
        }

        // QQHQ :: POLEXT
        [Authorize(Policy = "MustOwnImage")]
        [HttpPut("{id}")]
        public IActionResult UpdateImage(Guid id,
            [FromBody] ImageForUpdate imageForUpdate)
        {

            if (imageForUpdate == null)
            {
                return this.BadRequest();
            }

            if (!this.ModelState.IsValid)
            {
                // return 422 - Unprocessable Entity when validation fails
                return new UnprocessableEntityObjectResult(this.ModelState);
            }

            Entities.Image imageFromRepo = this._galleryRepository.GetImage(id);
            if (imageFromRepo == null)
            {
                return this.NotFound();
            }

            Mapper.Map(imageForUpdate, imageFromRepo);

            this._galleryRepository.UpdateImage(imageFromRepo);

            if (!this._galleryRepository.Save())
            {
                throw new Exception($"Updating image with {id} failed on save.");
            }

            return this.NoContent();
        }
    }
}