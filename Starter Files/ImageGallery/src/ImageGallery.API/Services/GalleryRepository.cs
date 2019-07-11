using ImageGallery.API.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageGallery.API.Services
{
    public class GalleryRepository : IGalleryRepository, IDisposable
    {
        GalleryContext _context;

        public GalleryRepository(GalleryContext galleryContext)
        {
            this._context = galleryContext;
        }

        public bool ImageExists(Guid id)
        {
            return this._context.Images.Any(i => i.Id == id);
        }

        public Image GetImage(Guid id)
        {
            return this._context.Images.FirstOrDefault(i => i.Id == id);
        }

        public IEnumerable<Image> GetImages()
        {
            return this._context.Images
                .OrderBy(i => i.Title).ToList();
        }

        public IEnumerable<Image> GetImages(string ownerId)
        {
            return this._context.Images
                .Where(i => i.OwnerId == ownerId)
                .OrderBy(i => i.Title).ToList();
        }

        public bool IsImageOwner(Guid id, string ownerId)
        {
            return this._context.Images.Any(i => i.Id == id && i.OwnerId == ownerId);
        }


        public void AddImage(Image image)
        {
            this._context.Images.Add(image);
        }

        public void UpdateImage(Image image)
        {
            // no code in this implementation
        }

        public void DeleteImage(Image image)
        {
            this._context.Images.Remove(image);

            // Note: in a real-life scenario, the image itself should also
            // be removed from disk.  We don't do this in this demo
            // scenario, as we refill the DB with image URIs (that require
            // the actual files as well) for demo purposes.
        }

        public bool Save()
        {
            return (this._context.SaveChanges() >= 0);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._context != null)
                {
                    this._context.Dispose();
                    this._context = null;
                }

            }
        }
    }
}