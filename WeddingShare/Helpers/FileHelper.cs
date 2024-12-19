using WeddingShare.Helpers.Database;

namespace WeddingShare.Helpers
{
    public interface IFileHelper
    {
        bool DirectoryExists(string path);
        bool CreateDirectoryIfNotExists(string path);
        bool DeleteDirectoryIfExists(string path, bool recursive = true);
        string[] GetDirectories(string path, string pattern = "*", SearchOption searchOption = SearchOption.AllDirectories);
        string[] GetFiles(string path, string pattern = "*.*", SearchOption searchOption = SearchOption.AllDirectories);
        bool FileExists(string path);
        bool DeleteFileIfExists(string path);
        bool MoveFileIfExists(string source, string destination);
        Task<byte[]> ReadAllBytes(string path);
        Task SaveFile(IFormFile file, string path, FileMode mode);
	}

    public class FileHelper : IFileHelper
    {
        public FileHelper()
        {
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public bool CreateDirectoryIfNotExists(string path)
        {
            if (!DirectoryExists(path))
            { 
                Directory.CreateDirectory(path);

                return true;
            }
                
            return false;
        }

        public bool DeleteDirectoryIfExists(string path, bool recursive = true)
        {
            if (DirectoryExists(path))
            {
                Directory.Delete(path, recursive);

                return true;
            }

            return false;
        }

        public string[] GetDirectories(string path, string pattern = "*", SearchOption searchOption = SearchOption.AllDirectories)
        {
            return Directory.GetDirectories(path, pattern, searchOption);
        }

        public string[] GetFiles(string path, string pattern = "*", SearchOption searchOption = SearchOption.AllDirectories)
        {
            return Directory.GetFiles(path, pattern, searchOption);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DeleteFileIfExists(string path)
        {
            if (FileExists(path))
            {
                File.Delete(path);

                return true;
            }

            return false;
        }

        public bool MoveFileIfExists(string source, string destination)
        {
            if (FileExists(source))
            {
                File.Move(source, destination);

                return true;
            }

            return false;
        }

        public async Task<byte[]> ReadAllBytes(string path)
        {
            return await File.ReadAllBytesAsync(path);
        }

        public async Task SaveFile(IFormFile file, string path, FileMode mode)
        {
			using (var fs = new FileStream(path, mode))
			{
				await file.CopyToAsync(fs);
			}
		}
    }
}