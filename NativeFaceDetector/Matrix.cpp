#include "pch.h"
#include "Matrix.h"

using namespace NativeFaceDetector;

Matrix::Matrix(Size size)
	: m_storage(new int[size.width * size.height]), m_size(size)
{
}

Matrix::~Matrix()
{
	delete[] m_storage;
}

void Matrix::resize(Size size)
{
	Size oldSize = m_size;
	m_size = size;
	if (size.width * size.height < oldSize.width * oldSize.height)
		return;

	delete[] m_storage;
	m_storage = new int[m_size.width * m_size.height];
}