import os
import shutil
import datetime
import asyncio
from pathlib import Path


def generate_random_hash(length=15):
    """Generate a random string of lowercase letters and digits."""
    return "".join(random.choices(string.ascii_lowercase + string.digits, k=length))


#Much better method to avoid conflicts:
async def rename_folders(target_folder):
    async with asyncio.Lock():  # Acquire a lock to avoid race conditions
        for filename in os.listdir(target_folder):
            filepath = os.path.join(target_folder, filename)
            if os.path.isdir(filepath) and filename.isdigit():
                success = False
                while not success:
                    new_name = f"{filename}_{generate_random_hash()}"
                    new_filepath = os.path.join(target_folder, new_name)
                    try:
                        os.rename(filepath, new_filepath)
                        success = True
                    except FileExistsError:
                        # If the new folder name already exists, wait for a random time before trying again
                        time.sleep(random.uniform(0.1, 0.5))


async def create_numbered_subdirs(target_folder, chunk_size=500):
    file_paths = []
    for root, dirs, files in os.walk(target_folder):
        for file in files:
            file_path = os.path.join(root, file)
            if os.path.isfile(file_path):
                file_paths.append(file_path)

    file_paths.sort(key=lambda x: os.path.getmtime(x))

    total_files = len(file_paths)
    num_chunks = (total_files + chunk_size - 1) // chunk_size

    for i in range(num_chunks):
        chunk_folder = os.path.join(target_folder, str(i+1))
        os.makedirs(chunk_folder, exist_ok=True)
        for file in file_paths[i*chunk_size : (i+1)*chunk_size]:
            src_path = file
            dst_path = os.path.join(chunk_folder, Path(file).name)
            await asyncio.get_running_loop().run_in_executor(None, shutil.move, src_path, dst_path)

async def delete_empty_folders(target_folder):
    # get list of all subdirectories in path
    subdirs = [os.path.join(target_folder, d) for d in os.listdir(target_folder) if os.path.isdir(os.path.join(target_folder, d))]

    # delete empty subdirectories
    for subdir in subdirs:
        if not os.listdir(subdir):
            await asyncio.get_running_loop().run_in_executor(None, os.rmdir, subdir)

async def main(target_folder, chunk_size):
    #await move_files_to_temp(target_folder)
    await rename_folders(target_folder)
    await create_numbered_subdirs(target_folder, chunk_size)
    await delete_empty_folders(target_folder)

if __name__ == '__main__':
    target_folder = input("Enter target folder path: ")
    chunk_size = int(input("Enter number of files for each folder: "))
    asyncio.run(main(target_folder, chunk_size))