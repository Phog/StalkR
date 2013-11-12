#include "pch.h"
#include "Tree.h"

using namespace NativeFaceDetector;

void Tree::setScale(float scale)
{
	for (std::vector<Feature>::iterator i = m_features.begin(); i < m_features.end(); ++i)
		i->setScale(scale);
}

float Tree::getVal(const Matrix &grayImage, const Matrix &squares, int i, int j) const
{
    const Feature *feature = &m_features[0];
    for (;;)
    {
        if (feature->chooseLeft(grayImage, squares, i, j))
        {
            if (feature->leftNode() <= 0)
                return feature->leftVal();

            feature = &m_features[feature->leftNode() - 1];
        }
        else
        {
            if (feature->rightNode() <= 0)
                return feature->rightVal();

			feature = &m_features[feature->rightNode() - 1];
        }
    }
}
