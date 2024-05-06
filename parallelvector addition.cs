#define CL_TARGET_OPENCL_VERSION 120
#include <CL/cl.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>
// Define the size of the vectors
#define SIZE 100000
// Function to create (or retrieve) an OpenCL device
cl_device_id create_device()
{
    cl_platform_id platform;
    cl_device_id dev;
    cl_int err;
    // Get platform information
    err = clGetPlatformIDs(1, &platform, NULL);
    if (err < 0)
    {
        perror("Couldn't identify a platform");
        exit(1);
    }
    // Get device information (prefer GPU, fallback to CPU)
    err = clGetDeviceIDs(platform, CL_DEVICE_TYPE_GPU, 1, &dev, NULL);
    if (err == CL_DEVICE_NOT_FOUND)
    {
        printf("GPU not found, using CPU\n");
        err = clGetDeviceIDs(platform, CL_DEVICE_TYPE_CPU, 1, &dev, NULL);
    }
    if (err < 0)
    {
        perror("Couldn't access any devices");
        exit(1);
    }
    return dev;
}
int main()
{
    // Initialize OpenCL variables
    cl_platform_id platform;
    cl_device_id device;
    cl_context context;
    cl_command_queue queue;
    cl_program program;
    cl_kernel kernel;
    cl_int err;
    // Create an OpenCL device
    device = create_device();
    // Create an OpenCL context
    context = clCreateContext(NULL, 1, &device, NULL, NULL, &err);
    if (err < 0)
    {
        perror("Couldn't create a context");
        exit(1);
    }
    // Create a command queue
    queue = clCreateCommandQueue(context, device, 0, &err);
    if (err < 0)
    {
        perror("Couldn't create a command queue");
        exit(1);
    }
    // Load and compile the OpenCL program
    FILE* program_handle;
    char* program_buffer;
    size_t program_size;
    program_handle = fopen("./vector_add.cl", "r");
    if (program_handle == NULL)
    {
        perror("Couldn't find the program file");
        exit(1);
    }
    fseek(program_handle, 0, SEEK_END);
    program_size = ftell(program_handle);
    rewind(program_handle);
    program_buffer = (char*)malloc(program_size + 1);
    program_buffer[program_size] = '\0';
    fread(program_buffer, sizeof(char), program_size, program_handle);
    fclose(program_handle);
    program = clCreateProgramWithSource(context, 1, (const char
    **)&program_buffer, &program_size, &err);
    if (err < 0)
    {
        perror("Couldn't create the program");
        exit(1);
    }
    err = clBuildProgram(program, 1, &device, NULL, NULL, NULL);
    if (err < 0)
    {
        perror("Couldn't build the program");
        exit(1);
    }
    // Create the OpenCL kernel
    kernel = clCreateKernel(program, "vector_add", &err);
    if (err < 0)
    {
        perror("Couldn't create the kernel");
        exit(1);
    }
    // Initialize host arrays
    int* A = (int*)malloc(SIZE * sizeof(int));
    int* B = (int*)malloc(SIZE * sizeof(int));
    int* C = (int*)malloc(SIZE * sizeof(int));
    // Initialize arrays with random values
    srand(time(NULL));
    for (int i = 0; i < SIZE; i++)
    {
        A[i] = rand() % 100;
        B[i] = rand() % 100;
    }
    // Create OpenCL buffers
    cl_mem buffer_A = clCreateBuffer(context, CL_MEM_READ_ONLY |
    CL_MEM_COPY_HOST_PTR, sizeof(int) * SIZE, A, &err);
    cl_mem buffer_B = clCreateBuffer(context, CL_MEM_READ_ONLY |
    CL_MEM_COPY_HOST_PTR, sizeof(int) * SIZE, B, &err);
    cl_mem buffer_C = clCreateBuffer(context, CL_MEM_WRITE_ONLY, sizeof(int) *
    SIZE, NULL, &err);
    if (err < 0)
    {
        perror("Couldn't create OpenCL buffers");
        exit(1);
    }
    // Set kernel arguments
    err = clSetKernelArg(kernel, 0, sizeof(cl_mem), &buffer_A);
    err |= clSetKernelArg(kernel, 1, sizeof(cl_mem), &buffer_B);
    err |= clSetKernelArg(kernel, 2, sizeof(cl_mem), &buffer_C);
    if (err < 0)
    {
        perror("Couldn't set kernel arguments");
        exit(1);
    }
    // Define global and local work sizes
    size_t global_work_size = SIZE;
    size_t local_work_size = 64; // Adjust according to device capabilities
                                 // Enqueue the kernel for execution
    err = clEnqueueNDRangeKernel(queue, kernel, 1, NULL, &global_work_size,
    &local_work_size, 0, NULL, NULL);
    if (err < 0)
    {
        perror("Couldn't enqueue the kernel");
        exit(1);
    }
    // Read the result from the device
    err = clEnqueueReadBuffer(queue, buffer_C, CL_TRUE, 0, sizeof(int) * SIZE,
    C, 0, NULL, NULL);
    if (err < 0)
    {
        perror("Couldn't read buffer C");
        exit(1);
    }
    // Clean up OpenCL resources
    clReleaseMemObject(buffer_A);
    clReleaseMemObject(buffer_B);
    clReleaseMemObject(buffer_C);
    clReleaseKernel(kernel);
    clReleaseProgram(program);
    clReleaseCommandQueue(queue);
    clReleaseContext(context);
    // Free host memory
    free(A);
    free(B);
    free(C);
    return 0;
}