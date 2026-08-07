#include "cross.h"
#include "unhsq.h"
#include "image_parser.h"

#include <fstream>
#include <iostream>
#include <string>
#include <png.h>
#include <boost/format.hpp>
#include <boost/program_options.hpp>
#include <boost/iostreams/stream.hpp>
#include <boost/iostreams/device/array.hpp>
#include <boost/iostreams/device/file.hpp>
#include <boost/algorithm/string/predicate.hpp>

using namespace std;
using boost::format;
namespace bio = boost::iostreams;
namespace po  = boost::program_options;

void stream_write_data(png_structp png_ptr, png_bytep data, png_size_t length)
{
	auto f = static_cast<ostream*>(png_get_io_ptr(png_ptr));
	f->write(reinterpret_cast<char*>(data), length);
}

void stream_flush_data(png_structp png_ptr)
{
	auto f = static_cast<ostream*>(png_get_io_ptr(png_ptr));
	f->flush();
}

void user_error_fn(png_structp png_ptr, png_const_charp error_msg)
{
	cerr << "[libpng ERROR] " << error_msg;
}

void user_warning_fn(png_structp png_ptr, png_const_charp warning_msg)
{
	cerr << "[libpng warning] " << warning_msg;
}

bool write_to_png(ostream& f, const sprite& spr)
{
	// Create PNG struct
	png_structp png_ptr = png_create_write_struct(PNG_LIBPNG_VER_STRING, NULL, user_error_fn, user_warning_fn);

	if (!png_ptr)
		return false;

	// Get PNG Info struct
	png_infop info_ptr = png_create_info_struct(png_ptr);
	if (!info_ptr)
	{
		png_destroy_write_struct(&png_ptr, (png_infopp)NULL);
		return false;
	}

	// Use custom IO: C++ IO Streams
	png_set_write_fn(png_ptr, &f, stream_write_data, stream_flush_data);

	// Set error handling
	if (setjmp(png_jmpbuf(png_ptr)))
	{
		png_destroy_write_struct(&png_ptr, &info_ptr);
		return false;
	}

	png_set_IHDR(png_ptr, info_ptr,spr.width, spr.height,
		8, PNG_COLOR_TYPE_RGB_ALPHA, PNG_INTERLACE_NONE,
		PNG_COMPRESSION_TYPE_DEFAULT, PNG_FILTER_TYPE_DEFAULT);

	png_write_info(png_ptr, info_ptr);

	const byte* row_ptr = reinterpret_cast<const byte*>(spr.pixels.data());
	for (size_t i = 0; i < spr.height; i++)
	{
		png_write_row(png_ptr, row_ptr);
		row_ptr += spr.width * 4;
	}

	png_destroy_write_struct(&png_ptr, &info_ptr);

	return true;
}

unique_ptr<istream> open_input_file(const std::string& filename)
{
	auto f = new bio::stream<bio::file_source>(filename, BOOST_IOS::binary);
	if (!(*f)->is_open())
		throw std::runtime_error((format("Input file not found: '%s'.") % filename).str());

	return unique_ptr<istream>(f);
}

unique_ptr<ostream> open_output_file(const std::string& inputFilename, int index)
{
	std::string filename;
	if (boost::iends_with(inputFilename, ".hsq"))
		filename.assign(inputFilename.begin(), inputFilename.end() - 4);
	else
		filename = inputFilename;

	filename += (boost::format("%03d.png") % index).str();
	
	auto f = new bio::stream<bio::file_sink>(filename, BOOST_IOS::binary);
	if (!(*f)->is_open())
		throw std::runtime_error((format("Could not create output file: '%s'.") % filename).str());

	return unique_ptr<ostream>(f);
}

void process_input_file(const po::variables_map& vm, const std::string& filename)
{
	vector<byte> decompressed;
	unique_ptr<istream> input = open_input_file(filename);

	if (!vm.count("uncompressed"))
	{
		decompressed = hsq::unpack_from_stream(*input);
		input.reset(new bio::stream<bio::array_source>(reinterpret_cast<const char*>(decompressed.data()), decompressed.size()));
	}

	image_parser p;
	int index = 0;
	p.parse(*input, [&filename, &index](const sprite& spr)
	{
		unique_ptr<ostream> output = open_output_file(filename, index++);
		write_to_png(*output, spr);
	});
}

int utf8_main(int argc, char* argv[])
{

	try
	{
		// Declare the supported options.
		po::options_description desc("Allowed options");
		desc.add_options()
			("help,h",                                      "show this help message")
			("uncompressed,u",                              "input file is already decompressed")
			("transparent,t",  po::value<string>(),         "transparent color")
			("input,i",        po::value<vector<string>>(), "input files");

		po::positional_options_description pargs;
		pargs.add("input", -1);

		po::variables_map vm;
		po::store(po::command_line_parser(argc, argv).options(desc).positional(pargs).run(), vm);
		po::notify(vm);

		if (vm.count("help"))
		{
			cout << desc << endl << endl;
			return 0;
		}

		if (!vm.count("input"))
		{
			cout << "Input file(s) must be specified." << endl;
			cout << "Call with -h to show help." << endl << endl;			
			return -1;
		}

		auto inputFiles = vm["input"].as<vector<string>>();
		for_each(inputFiles.begin(), inputFiles.end(), bind(process_input_file, ref(vm), placeholders::_1));
	}
	catch (std::exception& ex)
	{
		cerr << "Exception: " << ex.what() << endl << endl;
		return -1;
	}

	return 0;
}

WRAP_UTF8_MAIN()
