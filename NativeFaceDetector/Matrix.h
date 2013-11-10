#pragma once

#include "Structures.h"

namespace NativeFaceDetector
{

	class Matrix
	{
		int *m_storage;
		Size m_size;

		Matrix(const Matrix& rhs);
		Matrix& operator=(const Matrix& rhs);
	public:
		Matrix(Size size);
		~Matrix();
	
		void resize(Size size);
		Size size() const { return m_size; }

		const int& operator()(int i, int j) const { return m_storage[i * m_size.height + j]; }
		int& operator()(int i, int j) { return m_storage[i * m_size.height + j]; }
	};

}

